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
    [SerializeField] [Range(0.3f, 0.9f)] float duckHeightScale = 0.55f;

    public ParticleSystem dust;
    public Vector2 moveInput;
    public Rigidbody2D myRigidbody;
    public Animator myAnimator;
    public CapsuleCollider2D myCapsuleCollider;

    [Header("Respawn")]
    [Tooltip("How often the sprite toggles visibility while invulnerable, purely as a " +
        "visual cue that contact won't kill right now.")]
    [SerializeField] float invulnerabilityBlinkInterval = 0.1f;

    public bool IsDead { get; private set; }

    /// <summary>True for a short window after a checkpoint respawn (see GrantInvulnerability) -
    /// Hazard contact is ignored while this is set.</summary>
    public bool IsInvulnerable { get; private set; }

    int jumpsRemaining;
    float lastGroundedTime = -999f;
    int groundMask;
    float baseScaleMagnitude;
    SpriteRenderer spriteRenderer;
    Coroutine invulnerabilityRoutine;

    public bool IsDucking { get; private set; }
    float facingSign = 1f;
    float standingColliderHeight;
    float standingColliderOffsetY;

    void Start()
     {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myCapsuleCollider = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        groundMask = LayerMask.GetMask("Ground");
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
        Run();
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
        if (IsDead) return;
        if (IsDucking) return;
        if (!value.isPressed) return;
        if (jumpsRemaining <= 0) return;

        // Set (not add) vertical velocity so the second jump is a consistent height
        // regardless of whether the player is still rising or already falling.
        myRigidbody.linearVelocity = new Vector2(myRigidbody.linearVelocity.x, jumpSpeed);
        jumpsRemaining--;
        myAnimator.SetTrigger("jump");
        StopDust();
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
        while (elapsed < duration)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(invulnerabilityBlinkInterval);
            elapsed += invulnerabilityBlinkInterval;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        IsInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    /// <summary>True while actually moving horizontally, regardless of whether the player is
    /// grounded or mid-air. Used by camera effects (e.g. zoom-on-run) - reads the rigidbody's
    /// velocity rather than raw input so it stays false while ducking holds a direction but
    /// zeroes actual movement (see Run()).</summary>
    public bool IsRunning { get { return Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon; } }

    bool isRunning = false;

    void Run()
    {
        // Ducking plants the player in place (like most platformers - it's a dodge pose, not
        // a crouch-walk) rather than just slowing them down.
        float x = IsDucking ? 0f : moveInput.x;
        Vector2 playerVelocity = new Vector2(x * runSpeed, myRigidbody.linearVelocity.y);
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