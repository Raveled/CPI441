// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // **************************************************** //
    // *** Variable Initializations *** //

    Rigidbody rb;

    public float moveSpeed = 5f;
    public Vector2 MoveInput;

    public float jumpForce = 5f;
    bool jumpQueued;
    public float groundCheckDistance = 0.2f;
    public float distanceOffset = 0.1f;
    public LayerMask groundLayer;

    public InputActionReference move;
    public InputActionReference jump;

    // **************************************************** //
    // *** Monobehavior Functions *** //

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Ensures the rigidbody does not topple over at the start

        // Lock the cursor to the center of the screen & Hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //testing
        Vector3 origin = transform.position + Vector3.down * distanceOffset;
        bool grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
        //testing done

        MoveInput = move.action.ReadValue<Vector2>();

        // Get the velocity and input direction in local space, transform into world space and then set the final horizontal velocity
        Vector3 currentVel = rb.linearVelocity;

        Vector3 inputDir = new Vector3(MoveInput.x, 0f, MoveInput.y);

        Vector3 worldDir = transform.TransformDirection(inputDir.normalized);

        Vector3 horizontalVel = worldDir * moveSpeed;

        // Apply the velocity values to the rigidbody
        rb.linearVelocity = new Vector3(
            horizontalVel.x,
            currentVel.y,   // keep gravity / jump
            horizontalVel.z
        );

        if (jumpQueued && IsGrounded())
        {
            Debug.Log("Jumping!");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpQueued = false;
        }
    }

    // **************************************************** //
    // *** Jump Action *** //

    private void OnJump(InputAction.CallbackContext context)
    {
        if (IsGrounded())
        {
            Debug.Log("IsGrounded and Trying to Jump!");
            jumpQueued = true;
        }
    }

    public bool IsGrounded()
    {
        Debug.Log("IsGrounded!");
        Vector3 origin = transform.position + Vector3.down * distanceOffset;
        bool grounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer);
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, grounded ? Color.green : Color.red);
        return grounded;
    }

    // **************************************************** //
    // *** Handle Enabling and Disabling Actions *** //

    private void OnEnable()
    {
        jump.action.started += OnJump;
    }

    private void OnDisable()
    {
        jump.action.started -= OnJump;
    }
}
// ******************************************* //