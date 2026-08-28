using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float jumpSpeed = 5f;
    [SerializeField] int maxJumps = 2; // 1 = single jump, 2 = double jump

    public ParticleSystem dust;
    public Vector2 moveInput;
    public Rigidbody2D myRigidbody;
    public Animator myAnimator;
    public CapsuleCollider2D myCapsuleCollider;

    public bool IsDead { get; private set; }

    int jumpsRemaining;

    void Start()
     {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myCapsuleCollider = GetComponent<CapsuleCollider2D>();
        StopDust();
    }

    void Update()
    {
        if (IsDead) return;
        Run();
        FlipSprite();

        // Refill jumps only once grounded and no longer moving upward, so the tail end of a
        // jump's ascent (which can still briefly overlap the Ground layer) doesn't refill early.
        if (myRigidbody.velocity.y <= 0f && myCapsuleCollider.IsTouchingLayers(LayerMask.GetMask("Ground")))
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
        myRigidbody.velocity = new Vector2(myRigidbody.velocity.x, jumpSpeed);
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
        myRigidbody.velocity = Vector2.zero;
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
        Vector2 playerVelocity = new Vector2(moveInput.x * runSpeed, myRigidbody.velocity.y);
        myRigidbody.velocity = playerVelocity;
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.velocity.x) > Mathf.Epsilon;

        if (playerHasHorizontalSpeed && !isRunning && myCapsuleCollider.IsTouchingLayers(LayerMask.GetMask("Ground")))
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
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.velocity.x) > Mathf.Epsilon;
        if (playerHasHorizontalSpeed)
        {
            transform.localScale = new Vector2(Mathf.Sign(myRigidbody.velocity.x)*0.2f, 0.2f);
        }
    }

    void CreateDust()
    {
        dust.Play();
    }

    void StopDust()
    {
        dust.Stop();
    }
}