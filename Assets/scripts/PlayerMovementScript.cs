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

    public ParticleSystem dust;
    public Vector2 moveInput;
    public Rigidbody2D myRigidbody;
    public Animator myAnimator;
    public CapsuleCollider2D myCapsuleCollider;

    public bool IsDead { get; private set; }

    int jumpsRemaining;
    float lastGroundedTime = -999f;
    int groundMask;

    void Start()
     {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myCapsuleCollider = GetComponent<CapsuleCollider2D>();
        groundMask = LayerMask.GetMask("Ground");
        StopDust();
    }

    void Update()
    {
        if (IsDead) return;
        Run();
        FlipSprite();

        if (myCapsuleCollider.IsTouchingLayers(groundMask))
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
        if (IsDead) return;
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

    /// <summary>True while there's horizontal movement input, regardless of whether the
    /// player is grounded or mid-air. Used by camera effects (e.g. zoom-on-run).</summary>
    public bool IsRunning { get { return Mathf.Abs(moveInput.x) > Mathf.Epsilon; } }

    bool isRunning = false;

    void Run()
    {
        Vector2 playerVelocity = new Vector2(moveInput.x * runSpeed, myRigidbody.linearVelocity.y);
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

    
    void FlipSprite()
    {
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.linearVelocity.x) > Mathf.Epsilon;
        if (playerHasHorizontalSpeed)
        {
            transform.localScale = new Vector2(Mathf.Sign(myRigidbody.linearVelocity.x)*0.2f, 0.2f);
        }
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