using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] int maxJumps = 2; // 1 = single jump, 2 = double jump

    // Grace window after losing ground contact during which a jump/refill still counts as
    // "grounded". Covers the case where running fast over uneven ground (collider seams,
    // slight bumps) causes IsTouchingLayers to flicker false for a stray frame or two -
    // without this, that flicker can eat the jump refill and make jump feel like it randomly
    // stops working after running for a while.
    [SerializeField] float coyoteTime = 0.12f;

    [Header("Duck")]
    [Tooltip("Move.y is already bound to S/Down-arrow for both keyboard layouts, so ducking " +
        "reuses that input instead of needing a new action - hold down while grounded.")]
    [SerializeField] float duckInputThreshold = -0.5f;
    [Tooltip("Fraction of the standing collider height used while ducking - shrinks the hitbox " +
        "down and in from the top so overhead hazards that would otherwise clip the player can " +
        "be ducked under. Purely a gameplay hitbox change now - the visual crouch is a real " +
        "Animator state (Duck.anim, isDucking bool) driven from HandleDuck below, not a sprite " +
        "squash: an earlier squash-scale approach shrank toward the rig's pivot (not the feet), " +
        "which just looked like the character getting smaller instead of crouching.")]
    [SerializeField] [Range(0.3f, 0.9f)] float duckHeightScale = 0.35f;
    [Tooltip("Horizontal speed while ducking, as a fraction of runSpeed - a duck-walk, not a " +
        "full stop. Drives the Duck/DuckWalk Animator split via isRunning, same as standing.")]
    [SerializeField] [Range(0f, 1f)] float duckSpeedMultiplier = 0.5f;

    [Header("Dash")]
    [Tooltip("Reuses the existing \"Fire\" input action (Left Shift / Left-Click / Gamepad West " +
        "button) - it had no gameplay use yet, so no new binding UI is needed. A short, fast " +
        "horizontal burst in the direction the player is facing: crosses gaps, dodges harpoons/" +
        "nets, and grants a brief invulnerability window (see GrantInvulnerability) so a " +
        "well-timed dash can punch straight through a hazard instead of just outrunning it.")]
    [SerializeField] float dashSpeed = 20f;
    [SerializeField] float dashDuration = 0.16f;
    [SerializeField] float dashCooldown = 0.9f;
    [Tooltip("No bespoke dash animation/bone pose - the run cycle keeps playing (forced on for " +
        "the burst) and a fading afterimage trail sells the speed instead. Cheaper than a new " +
        "Animator state and avoids repeating the long duck-pose iteration cycle for a move whose " +
        "whole point is being over almost instantly.")]
    [SerializeField] [Range(2, 8)] int dashGhostCount = 5;
    [SerializeField] Color dashGhostTint = new Color(1f, 0.85f, 0.5f, 0.55f);
    [SerializeField] float dashGhostFadeTime = 0.25f;

    public ParticleSystem dust;
    public Vector2 moveInput;
    public Rigidbody2D myRigidbody;
    public Animator myAnimator;
    public CapsuleCollider2D myCapsuleCollider;

    [Header("Respawn")]
    [Tooltip("How often the sprite toggles visibility while invulnerable, purely as a " +
        "visual cue that contact won't kill right now.")]
    [SerializeField] float invulnerabilityBlinkInterval = 0.1f;

    /// <summary>Only one Koli Girl exists per level, so a simple static reference (set in
    /// Awake, same pattern as GameManager.Instance) is enough for the touch-controls buttons -
    /// see TouchTapButton/TouchMovementController - to find her without a per-scene serialized
    /// reference that would need re-wiring in every level.</summary>
    public static PlayerMovementScript Instance { get; private set; }

    public bool IsDead { get; private set; }

    /// <summary>True for a short window after a checkpoint respawn (see GrantInvulnerability) -
    /// Hazard contact is ignored while this is set.</summary>
    public bool IsInvulnerable { get; private set; }

    int jumpsRemaining;
    float lastGroundedTime = -999f;
    int groundMask;
    float baseScaleMagnitude;

    // Koli Girl is a composite bone rig (Body/Face/Scarf/Basket/limbs etc., each its own
    // SpriteRenderer under a bone) rather than one sprite - GetComponent<SpriteRenderer>() on
    // this object finds only the root's own renderer, which the PSB import leaves disabled with
    // no sprite assigned (unused). Both the invulnerability blink and the dash ghost trail need
    // every visible part, not that empty one.
    SpriteRenderer[] bodyPartRenderers;
    Coroutine invulnerabilityRoutine;

    public bool IsDucking { get; private set; }
    float facingSign = 1f;
    float standingColliderHeight;
    float standingColliderOffsetY;

    public bool IsDashing { get; private set; }
    float dashCooldownTimer;
    float dashDirection = 1f;

    [Header("Net Snare")]
    [Tooltip("Speed multiplier applied on top of the normal run speed while snared by a " +
        "FishingNet - a crawl, not a full stop, so struggling out still feels like doing " +
        "something.")]
    [SerializeField] [Range(0f, 0.6f)] float netSnareSpeedMultiplier = 0.15f;
    float netSnareTimer;

    /// <summary>True while caught in a FishingNet (see ApplySnare). Blocks Jump and Dash the
    /// same way IsDucking already does, so a snared player can't just skip past the penalty.</summary>
    public bool IsSnared => netSnareTimer > 0f;

    [Header("Wind")]
    /// <summary>Set/cleared by WindGust while the player is inside its trigger. Applied in Run()
    /// as a flat addition to the target x-velocity rather than through Rigidbody2D directly -
    /// Run() already sets velocity.x fresh from moveInput every Update(), so anything added any
    /// other way would just get overwritten the same frame.</summary>
    float windPush;

    void Awake()
    {
        Instance = this;
    }

    void Start()
     {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myCapsuleCollider = GetComponent<CapsuleCollider2D>();
        groundMask = LayerMask.GetMask("Ground");

        var allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        var withSprite = new List<SpriteRenderer>(allRenderers.Length);
        foreach (var r in allRenderers)
        {
            if (r.sprite != null) withSprite.Add(r);
        }
        bodyPartRenderers = withSprite.ToArray();
        // Cache once, at whatever scale this character was placed at - FlipSprite() below
        // only ever flips the sign, never overwrites the magnitude, so this works for any
        // character's own scale (not hardcoded to Koli Girl's 0.2) and can't drift even if
        // something reparents this object under a differently-scaled transform later.
        baseScaleMagnitude = Mathf.Abs(transform.localScale.x);
        standingColliderHeight = myCapsuleCollider.size.y;
        standingColliderOffsetY = myCapsuleCollider.offset.y;
        StopDust();
    }

    void Update()
    {
        if (IsDead) return;

        bool grounded = myCapsuleCollider.IsTouchingLayers(groundMask);
        HandleDuck(grounded);

        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
        if (netSnareTimer > 0f) netSnareTimer -= Time.deltaTime;

        if (IsDashing)
        {
            // Horizontal burst only - leaves whatever vertical velocity (falling, rising out of
            // a jump) alone, so dash reads as "boost" rather than "float".
            myRigidbody.linearVelocity = new Vector2(dashDirection * dashSpeed, myRigidbody.linearVelocity.y);
            myAnimator.SetBool("isRunning", true);
        }
        else
        {
            Run();
        }

        FlipSprite();

        if (grounded)
        {
            lastGroundedTime = Time.time;
        }

        // Refill jumps once grounded (within the coyote-time grace window) and no longer
        // moving upward, so the tail end of a jump's ascent doesn't refill early.
        bool recentlyGrounded = Time.time - lastGroundedTime <= coyoteTime;
        if (myRigidbody.linearVelocity.y <= 0f && recentlyGrounded)
        {
            jumpsRemaining = maxJumps;
        }
    }

    void OnMove(InputValue value)
    {
        if (IsDead) return;
        moveInput = value.Get<Vector2>();
        Debug.Log(moveInput);
    }

    void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        PerformJump();
    }

    /// <summary>Touch-UI jump button calls this directly (see TouchTapButton) instead of going
    /// through the Input System's InputValue plumbing - there's no bound control path for a
    /// screen tap, so the on-screen button is its own input source rather than feeding a virtual
    /// device. Same guards and effect as the keyboard/gamepad path via OnJump above.</summary>
    public void TouchJump()
    {
        PerformJump();
    }

    void PerformJump()
    {
        if (IsDead) return;
        if (IsDucking) return;
        if (IsSnared) return;
        if (jumpsRemaining <= 0) return;

        // Set (not add) vertical velocity so the second jump is a consistent height
        // regardless of whether the player is still rising or already falling.
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpSpeed);
        jumpsRemaining--;
        myAnimator.SetTrigger("jump");
        StopDust();
    }

    void OnFire(InputValue value)
    {
        if (IsDead) return;
        if (!value.isPressed) return;
        TryDash();
    }

    /// <summary>Touch-UI dash button calls this directly - same reasoning as TouchJump above.</summary>
    public void TouchDash()
    {
        if (IsDead) return;
        TryDash();
    }

    void TryDash()
    {
        // Blocked while ducking (collider is already shrunk for an overhead hazard - a dash
        // burst on top of that hitbox is more complexity than it's worth), while snared, and
        // during cooldown.
        if (IsDashing || IsDucking || IsSnared || dashCooldownTimer > 0f) return;

        dashDirection = facingSign;
        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        IsDashing = true;
        dashCooldownTimer = dashCooldown;
        // Slightly longer than the dash itself, so the very last frame of the burst (still
        // technically overlapping whatever hazard the player just dashed through) is covered too.
        GrantInvulnerability(dashDuration + 0.05f);

        float elapsed = 0f;
        float ghostInterval = dashDuration / dashGhostCount;
        float sinceLastGhost = ghostInterval; // so the first ghost spawns immediately, not after one interval

        while (elapsed < dashDuration)
        {
            sinceLastGhost += Time.deltaTime;
            if (sinceLastGhost >= ghostInterval)
            {
                SpawnGhost();
                sinceLastGhost = 0f;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        IsDashing = false;
    }

    void SpawnGhost()
    {
        if (bodyPartRenderers == null || bodyPartRenderers.Length == 0) return;

        var ghostObj = new GameObject("DashGhost");
        var ghost = ghostObj.AddComponent<DashGhostFade>();
        ghost.Init(bodyPartRenderers, dashGhostTint, dashGhostFadeTime);
    }

    /// <summary>Called by Hazard on contact (Fire, Spikes, ...). Stops control and plays
    /// the existing Death animation via the animator's "Dying" trigger, then hands off
    /// to GameManager to restart the level.</summary>
    public void Die()
    {
        if (IsDead || IsInvulnerable) return;
        IsDead = true;

        moveInput = Vector2.zero;
        myRigidbody.linearVelocity = Vector2.zero;
        myAnimator.SetTrigger("Dying");
        StopDust();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied();
        }
    }

    /// <summary>Called by GameManager right after repositioning the player at a checkpoint
    /// respawn. Without this, respawning directly into a patrolling enemy's path (or a rising
    /// tide) can kill the player again before they've had any chance to react and move -
    /// this gives them a short hazard-immune window instead, with a sprite flicker so it's
    /// obvious it's temporary.</summary>
    public void GrantInvulnerability(float duration)
    {
        if (invulnerabilityRoutine != null) StopCoroutine(invulnerabilityRoutine);
        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine(duration));
    }

    IEnumerator InvulnerabilityRoutine(float duration)
    {
        IsInvulnerable = true;
        float elapsed = 0f;
        bool visible = true;
        while (elapsed < duration)
        {
            visible = !visible;
            SetBodyVisible(visible);
            yield return new WaitForSeconds(invulnerabilityBlinkInterval);
            elapsed += invulnerabilityBlinkInterval;
        }

        SetBodyVisible(true);
        IsInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    void SetBodyVisible(bool visible)
    {
        if (bodyPartRenderers == null) return;
        foreach (var r in bodyPartRenderers)
        {
            if (r != null) r.enabled = visible;
        }
    }

    /// <summary>True while actually moving horizontally, regardless of whether the player is
    /// grounded or mid-air. Used by camera effects (e.g. zoom-on-run) - reads the rigidbody's
    /// velocity rather than raw input so it stays false while ducking holds a direction but
    /// zeroes actual movement (see Run()).</summary>
    public bool IsRunning { get { return Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon; } }

    bool isRunning = false;

    void Run()
    {
        // Ducking slows movement rather than freezing it, so the hitbox stays shrunk (still
        // dodging an overhead hazard) while duck-walking underneath it. A net snare stacks with
        // duck (crawling exists mostly so struggling still feels like doing *something*).
        float speedMultiplier = IsDucking ? duckSpeedMultiplier : 1f;
        if (IsSnared) speedMultiplier *= netSnareSpeedMultiplier;
        float x = moveInput.x * speedMultiplier;
        Vector2 playerVelocity = new Vector2(x * runSpeed + windPush, myRigidbody.linearVelocity.y);
        myRigidbody.linearVelocity = playerVelocity;
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon;

        if (playerHasHorizontalSpeed && !isRunning && myCapsuleCollider.IsTouchingLayers(groundMask))
        {
            CreateDust();
            isRunning = true;
        }
        else if (!playerHasHorizontalSpeed && isRunning)
        {
            StopDust();
            isRunning = false;
        }

        myAnimator.SetBool("isRunning", playerHasHorizontalSpeed);
    }

    /// <summary>Reuses the existing Move.y axis (already bound to S/Down-arrow) rather than a
    /// new input action - down while grounded shrinks the collider from the top (feet stay
    /// planted) so an overhead hazard that would otherwise clip the player can be ducked under,
    /// and drives the Animator's isDucking bool for the actual crouch pose.</summary>
    void HandleDuck(bool grounded)
    {
        // Only re-check grounded to START ducking, not to keep ducking - see git history for
        // why re-checking it every frame here is a bad idea (a collider-resize/grounded
        // feedback loop that made the character shake in place).
        bool wantsDuck = IsDucking
            ? moveInput.y < duckInputThreshold
            : grounded && moveInput.y < duckInputThreshold;
        if (wantsDuck == IsDucking) return;

        IsDucking = wantsDuck;
        myAnimator.SetBool("isDucking", IsDucking);

        float duckedHeight = standingColliderHeight * (IsDucking ? duckHeightScale : 1f);
        myCapsuleCollider.size = new Vector2(myCapsuleCollider.size.x, duckedHeight);
        myCapsuleCollider.offset = new Vector2(
            myCapsuleCollider.offset.x,
            standingColliderOffsetY - (standingColliderHeight - duckedHeight) * 0.5f);
    }

    void FlipSprite()
    {
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon;
        if (playerHasHorizontalSpeed)
        {
            facingSign = Mathf.Sign(myRigidbody.linearVelocity.x);
        }

        transform.localScale = new Vector3(facingSign * baseScaleMagnitude, baseScaleMagnitude, transform.localScale.z);
    }

    /// <summary>Called by FishingNet on contact. Refreshes (doesn't stack) the snare timer, so
    /// lingering in a net doesn't extend the penalty past snareDuration.</summary>
    public void ApplySnare(float duration)
    {
        netSnareTimer = Mathf.Max(netSnareTimer, duration);
    }

    /// <summary>Called by WindGust every physics step the player is inside its trigger (0 on
    /// exit). See the windPush field comment for why this goes through Run() instead of touching
    /// Rigidbody2D directly.</summary>
    public void SetWindPush(float push)
    {
        windPush = push;
    }

    void CreateDust()
    {
        dust.Play();
    }

    void StopDust()
    {
        dust.Stop();
        isRunning = false;
    }
}