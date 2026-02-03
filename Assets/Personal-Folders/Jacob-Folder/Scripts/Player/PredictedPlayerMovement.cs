using PurrNet;
using PurrNet.Prediction;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PredictedPlayerMovement : PredictedIdentity<PredictedPlayerMovement.MoveInput, PredictedPlayerMovement.MoveState>
{
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private PredictedRigidbody _rigidbody;

    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private float jumpCooldownTime = 1f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float planarDamping = 10f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    public Vector2 moveVector;
    public InputAction moveAction;
    public InputAction jumpAction;

    protected override void LateAwake()
    {
        if (isOwner)
        {
            moveAction = InputSystem.actions.FindAction("Move");
            jumpAction = InputSystem.actions.FindAction("Jump");

            moveAction?.Enable();
            jumpAction?.Enable();

            _playerCamera.Init();
        }
    }

    protected override void OnDestroy() 
    {
        base.OnDestroy();

        if (isOwner)
        {
            moveAction?.Disable();
            jumpAction?.Disable();
        }
    }


    protected override void Simulate(MoveInput input, ref MoveState state, float delta)
    {
        state.jumpCooldown -= delta;

        // Movement
        Vector3 targetVelocity = (transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x) * moveSpeed;
        Vector3 velocityDelta = targetVelocity - _rigidbody.linearVelocity;
        velocityDelta.y = 0f;

        _rigidbody.AddForce(velocityDelta * acceleration, ForceMode.Acceleration);

        var horizontal = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
        _rigidbody.AddForce(-horizontal * planarDamping);
        if (horizontal.magnitude > moveSpeed)
        {
            _rigidbody.velocity = new Vector3(targetVelocity.x, _rigidbody.velocity.y, targetVelocity.z);
        }

        // Jumping
        if(input.jump && IsGrounded() && state.jumpCooldown <= 0f)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            state.jumpCooldown = jumpCooldownTime;
        }

        // Rotation to face movement direction
        var camForward = input.cameraForward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > 0.001f)
        {
            _rigidbody.MoveRotation(Quaternion.LookRotation(camForward.normalized));
        }
    }

    private static Collider[] groundColliders = new Collider[8];
    private bool IsGrounded()
    {
        var hit = Physics.OverlapSphereNonAlloc(transform.position, groundCheckDistance, groundColliders, groundLayer);
        return hit > 0;
    }

    protected override void UpdateInput(ref MoveInput input)
    {
        input.jump |= jumpAction.WasPressedThisFrame(); // If jump is pressed this frame or jump was already true, keep it true
    }

    protected override void GetFinalInput(ref MoveInput input)
    {
        input.moveDirection = moveAction.ReadValue<Vector2>(); 
        input.cameraForward = _playerCamera.forward;
    }

    protected override void SanitizeInput(ref MoveInput input)
    {
        if (input.moveDirection.magnitude > 1f)
        {
            input.moveDirection.Normalize();
        }
    }

    public struct MoveInput : IPredictedData
    {
        public Vector2 moveDirection;
        public Vector3 cameraForward;
        public bool jump;

        public void Dispose() {}
    }

    public struct MoveState : IPredictedData<MoveState>
    {
        public float jumpCooldown;
        public void Dispose() {}
    }
}