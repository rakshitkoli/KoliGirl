using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementScript : MonoBehaviour
{
    // [SerializeField] float runSpeed = 10f;


    Vector2 moveInput;
    Rigidbody2D myRigidbody;

    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
    }

    void update()
    {
        Run();
    } 

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        Debug.Log(moveInput);
    }    

    void Run()
    {
        // Vector2 playerVelocity = new Vector2(moveInput.x * runSpeed, Rigidbody.velocity.y); 
        myRigidbody.velocity = moveInput;   
    }

}
