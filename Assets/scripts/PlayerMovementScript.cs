using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    [SerializeField] float runSpeed = 10f;
    [SerializeField] float jumpSpeed = 5f;

    public ParticleSystem dust;
    public Vector2 moveInput;
    public Rigidbody2D myRigidbody;
    public Animator myAnimator;
    public CapsuleCollider2D myCapsuleCollider;
    
    void Start() 
     {
        myRigidbody = GetComponent<Rigidbody2D>();  
        myAnimator = GetComponent<Animator>();
        myCapsuleCollider = GetComponent<CapsuleCollider2D>();
        StopDust();
    }

    void Update()
    {
        Run();
        FlipSprite();
    } 

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        Debug.Log(moveInput);
    }    

    void OnJump(InputValue value)
    {
        if(!myCapsuleCollider.IsTouchingLayers(LayerMask.GetMask("Ground"))) { return;}
        if(value.isPressed)
        {
           myRigidbody.velocity += new Vector2 (0f,jumpSpeed);
           myAnimator.SetTrigger("jump");
           StopDust();
        }
    }

    bool isRunning = false;

    void Run()
    {
        Vector2 playerVelocity = new Vector2(moveInput.x * runSpeed, myRigidbody.velocity.y);
        myRigidbody.velocity = playerVelocity;
        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.velocity.x) > Mathf.Epsilon;

        if (playerHasHorizontalSpeed && !isRunning)
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