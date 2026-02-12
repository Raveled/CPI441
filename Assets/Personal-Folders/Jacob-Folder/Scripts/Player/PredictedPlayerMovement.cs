using PurrNet;
using PurrNet.Prediction;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PredictedPlayerMovement : PredictedIdentity<PredictedPlayerMovement.MoveInput, PredictedPlayerMovement.MoveState>
{
    [Header("References")]
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] private PredictedRigidbody _rigidbody;
    [SerializeField] private Player _player;

    [Header("Movement Settings - Pull from SO_EntityStatBlock")]
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float jumpForce = 0f;
    [SerializeField] private float jumpCooldownTime = 0f;
    [SerializeField] private float acceleration = 0f;
    [SerializeField] private float planarDamping = 0f;

    [Header("Ground Check Settings")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;

    public Vector2 moveVector;
    public InputAction moveAction;
    public InputAction jumpAction;

    public string debugString = "test";

    protected override void LateAwake()
    {
        if (_player == null) _player = GetComponent<Player>();

        if (_player != null)
        {
            LoadStatsFromPlayer();
        }
        else
        {
            Debug.LogError("PredictedPlayerMovement: No Player component found on the GameObject.");
        }

        if (isOwner)
        {
            moveAction = InputSystem.actions.FindAction("Move");
            jumpAction = InputSystem.actions.FindAction("Jump");

            moveAction?.Enable();
            jumpAction?.Enable();

            _playerCamera.Init();
        }
    }

    private void LoadStatsFromPlayer()
    {
        SO_EntityStatBlock playerStatBlock = _player.GetEntityStatblock();
        moveSpeed = playerStatBlock.BaseMoveSpeed;
        jumpForce = playerStatBlock.BaseJumpForce;
        jumpCooldownTime = playerStatBlock.BaseJumpCooldown;
        acceleration = playerStatBlock.BaseAcceleration;
        planarDamping = playerStatBlock.BasePlanarDamping;
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

    protected override MoveState GetInitialState()
    {
        return new MoveState
        {
            position = transform.position,
            rotation = transform.rotation,
            velocity = _rigidbody.linearVelocity,
            jumpCooldown = 0f
        };
    }

    protected override void GetUnityState(ref MoveState state)
    {
        state.position = transform.position;
        state.rotation = transform.rotation;
        state.velocity = _rigidbody.linearVelocity;
    }

    protected override void SetUnityState(MoveState state)
    {
        transform.SetPositionAndRotation(state.position, state.rotation);
        _rigidbody.linearVelocity = state.velocity;
    }

    protected override void Simulate(MoveInput input, ref MoveState state, float delta)
    {
        state.jumpCooldown -= delta;

        // Movement
        Vector3 targetVelocity = (transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x) * moveSpeed;
        Vector3 velocityDelta = targetVelocity - state.velocity;
        velocityDelta.y = 0f;

        _rigidbody.AddForce(velocityDelta * acceleration, ForceMode.Acceleration);

        var horizontal = new Vector3(state.velocity.x, 0f, state.velocity.z);
        _rigidbody.AddForce(-horizontal * planarDamping);
        if (horizontal.magnitude > moveSpeed)
        {
            state.velocity = new Vector3(targetVelocity.x, state.velocity.y, targetVelocity.z);
            _rigidbody.linearVelocity = state.velocity;
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
            state.rotation = Quaternion.LookRotation(camForward.normalized);
            _rigidbody.MoveRotation(state.rotation);
        }

        // Update state velocity after physics
        state.velocity = _rigidbody.linearVelocity;
        state.position = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        // Sync stats from Player component in case they were updated (e.g., from leveling up or buffs)
        LoadStatsFromPlayer();
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
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float jumpCooldown;
        public void Dispose() {}
    }
}