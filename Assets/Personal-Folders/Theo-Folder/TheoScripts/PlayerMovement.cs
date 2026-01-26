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
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Keep Interpolate, NOT Extrapolate

        // Add these physics settings
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MoveInput = move.action.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        Vector3 inputDir = new Vector3(MoveInput.x, 0f, MoveInput.y);
        Vector3 worldDir = transform.TransformDirection(inputDir.normalized);

        // CRITICAL: Set velocity directly, don't modify existing velocity
        rb.linearVelocity = new Vector3(
            worldDir.x * moveSpeed,
            rb.linearVelocity.y, // Preserve vertical velocity for gravity/jumping
            worldDir.z * moveSpeed
        );

        if (jumpQueued && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
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