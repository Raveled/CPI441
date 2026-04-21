using PurrNet;
using PurrNet.Prediction;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PredictedPlayerMovement : PredictedIdentity<PredictedPlayerMovement.MoveInput, PredictedPlayerMovement.MoveState>
{
    [Header("References")]
    [SerializeField] private PlayerCamera _playerCamera;
    [SerializeField] public PredictedRigidbody _rigidbody;
    [SerializeField] public Player _player;
    [SerializeField] public GameObject firingPoint;

    [SerializeField] private GameObject playerObj;
    [SerializeField] private GameObject visualRoot;

    [Header("Movement Settings - Pull from SO_EntityStatBlock")]
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float jumpForce = 0f;
    [SerializeField] private float jumpCooldownTime = 0f;

    [SerializeField] private float acceleration = 0f;
    [SerializeField] private float planarDamping = 0f;

    [Header("Grounding")]
    [SerializeField] private GameObject groundCheckObject;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float groundCheckRadius = 0.5f;
    [SerializeField] private bool grounded;
    [SerializeField] private LayerMask groundLayer;

    [Header("Butterfly Fly Runtime")]
    [SerializeField] private bool butterflyFlyActive = false;
    [SerializeField] private Vector3 butterflyFlyDirection = Vector3.forward;
    [SerializeField] private float butterflyFlyDistance = 8f;
    [SerializeField] private float butterflyFlyDuration = 0.35f;
    [SerializeField] private Vector3 butterflyFlyHeightOffset = new Vector3(0f, 5f, 0f);

    private float butterflyFlyTimer = 0f;
    private Vector3 butterflyFlyStartPosition;
    private Butterfly butterflyAbility;

    [Header("Beetle Horn Runtime")]
    [SerializeField] private bool beetleHornImpaleActive = false;
    [SerializeField] private Vector3 beetleHornDirection = Vector3.forward;
    [SerializeField] private float beetleHornDistance = 4f;
    [SerializeField] private float beetleHornDuration = 0.4f;

    private float beetleHornTimer = 0f;
    private Vector3 beetleHornStartPosition;
    private Beetle beetleAbility;

    [Header("Beetle Roll Runtime")]
    [SerializeField] private bool beetleRollActive = false;
    [SerializeField] private Vector3 beetleRollDirection = Vector3.forward;
    [SerializeField] private float beetleRollSpeed = 15f;
    [SerializeField] private float beetleRollDuration = 1f;

    private float beetleRollTimer = 0f;

    // Input variables

    public Vector2 moveVector;
    public InputAction moveAction;
    public InputAction jumpAction;

    public SO_EntityStatBlock stats;

    protected override void LateAwake()
    {
        if (_player == null)
            _player = GetComponentInChildren<Player>();

        if (_player == null && isServer)
        {
            //Debug.Log($"Player {owner.Value} spawning playerRoot prefab");
            GameObject playerObject = Instantiate(playerObj, this.transform);
            playerObject.transform.SetParent(this.transform);

            _player = playerObject.GetComponent<Player>();
            
            if (_player != null)
            {
                _player.predictedMovement = this;
                _player.GiveOwnership(owner.Value);
                _player.SetStatblock(stats);
            }

            if (visualRoot != null)
            {
                visualRoot.transform.SetParent(this.transform);
            }
        }

        _rigidbody.isKinematic = false;

        if (isOwner)
        {
            moveAction = InputSystem.actions.FindAction("Move");
            jumpAction = InputSystem.actions.FindAction("Jump");
            moveAction?.Enable();
            jumpAction?.Enable();
            _playerCamera.Init();
        }

        //butterfly handling for flying / dash
        butterflyAbility = GetComponentInChildren<Butterfly>();

        //beetle handling for most attacks
        beetleAbility = GetComponentInChildren<Beetle>();
    }

    public void LoadStatsFromPlayer()
    {
        if (_player.GetEntityStatblock() == null)
            return;

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

        // ****************************** butterfly fly handling (overrides normal movement) ************************* // 
        if (butterflyFlyActive)
        {
            butterflyFlyTimer -= delta;

            float elapsed = butterflyFlyDuration - Mathf.Max(0f, butterflyFlyTimer);
            float t = Mathf.Clamp01(elapsed / butterflyFlyDuration);

            Vector3 targetPosition = Vector3.Lerp(
                butterflyFlyStartPosition,
                butterflyFlyStartPosition + butterflyFlyDirection * butterflyFlyDistance + butterflyFlyHeightOffset,
                t
            );

            state.position = targetPosition;
            state.velocity = Vector3.zero;
            state.rotation = Quaternion.LookRotation(butterflyFlyDirection.sqrMagnitude > 0.001f ? butterflyFlyDirection : transform.forward);

            transform.SetPositionAndRotation(state.position, state.rotation);
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.MoveRotation(state.rotation);

            if (butterflyFlyTimer <= 0f)
            {
                butterflyFlyActive = false;

                if (isServer && butterflyAbility != null)
                    butterflyAbility.NotifyFlyEndedFromMovement();
            }

            return;
        }

        // ****************************** beetle horn handling (overrides normal movement) ************************* //
        if (beetleHornImpaleActive)
        {
            beetleHornTimer -= delta;

            float elapsed = beetleHornDuration - Mathf.Max(0f, beetleHornTimer);
            float t = Mathf.Clamp01(elapsed / beetleHornDuration);

            Vector3 targetPosition = Vector3.Lerp(
                beetleHornStartPosition,
                beetleHornStartPosition + beetleHornDirection * beetleHornDistance,
                t
            );

            state.position = targetPosition;
            state.velocity = Vector3.zero;
            state.rotation = Quaternion.LookRotation(beetleHornDirection.sqrMagnitude > 0.001f ? beetleHornDirection : transform.forward);

            transform.SetPositionAndRotation(state.position, state.rotation);
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.MoveRotation(state.rotation);

            if (beetleHornTimer <= 0f)
            {
                beetleHornImpaleActive = false;

                if (isServer && beetleAbility != null)
                    beetleAbility.NotifyHornImpaleEndedFromMovement();
            }

            return;
        }

        // ****************************** normal movement handling ************************* // 
        state.jumpCooldown -= delta;

        state.isGrounded = CheckGrounded(state.position);
        if (state.isGrounded && state.velocity.y <= 0f)
        {
            state.velocity.y = Mathf.Max(state.velocity.y, -2f); // small downward bias
        }

        // Movement
        Vector3 targetVelocity = (transform.forward * input.moveDirection.y + transform.right * input.moveDirection.x) * moveSpeed;
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        Vector3 velocityDelta = targetVelocity - currentVelocity;
        velocityDelta.y = 0f;

        _rigidbody.AddForce(velocityDelta * acceleration, ForceMode.Acceleration);

        var horizontal = new Vector3(state.velocity.x, 0f, state.velocity.z);
        _rigidbody.AddForce(-horizontal * planarDamping * (1f - input.moveDirection.sqrMagnitude));
        if (horizontal.magnitude > moveSpeed)
        {
            state.velocity = new Vector3(targetVelocity.x, state.velocity.y, targetVelocity.z);
            _rigidbody.linearVelocity = state.velocity;
        }

        // Jumping
        if(input.jump && state.isGrounded && state.jumpCooldown <= 0f)
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

        if (_player == null)
        {
            _player = GetComponentInChildren<Player>();
            if (_player == null)
                Debug.LogWarning($"PredictedPlayerMovement on {name} could not find a Player component in Update. If this appears when a player is spawning, it is expected, since there is a slight delay.\n" +
                    $"If you are concerned about a character not having access to the 'player' component, uncomment the debug check in 'update' in this file! - Theo", this);
            return;
        }

        // *** DEBUG *** FIXED BY THEO - RESTORE TO CHECK ANY ISSUES WITH PLAYER COMPONENT ASSIGNMENT! *** //
        /*if (_player != null)
        {
            Debug.Log("PredictedPlayerMovement successfully found Player component in Update.");
        }*/

        // Sync stats from Player component in case they were updated (e.g., from leveling up or buffs)
        LoadStatsFromPlayer();
    }

    private static Collider[] groundColliders = new Collider[8];
    private bool CheckGrounded(Vector3 statePosition)
    {
        Vector3 origin = statePosition + Vector3.down * groundCheckObject.transform.localPosition.y;
        int hits = Physics.OverlapSphereNonAlloc(origin, groundCheckRadius, groundColliders, groundLayer, QueryTriggerInteraction.Ignore);
        grounded = hits > 0;
        return grounded;
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

    //Butterfly - flying/dashing handling

    public void StartButterflyFly(Vector3 direction, float distance, float duration)
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        butterflyFlyActive = true;
        butterflyFlyDirection = direction.normalized;
        butterflyFlyDistance = distance;
        butterflyFlyDuration = Mathf.Max(0.01f, duration);
        butterflyFlyTimer = butterflyFlyDuration;
        butterflyFlyStartPosition = transform.position;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector3.zero;

        Debug.Log($"[PredictedPlayerMovement] Butterfly Fly started. dir={butterflyFlyDirection}, distance={butterflyFlyDistance}, duration={butterflyFlyDuration}");
    }

    public void StopButterflyFly()
    {
        butterflyFlyActive = false;
        butterflyFlyTimer = 0f;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector3.zero;

        Debug.Log("[PredictedPlayerMovement] Butterfly Fly stopped.");
    }
    public void StartBeetleHornImpale(Vector3 direction, float distance, float duration)
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        beetleHornImpaleActive = true;
        beetleHornDirection = direction.normalized;
        beetleHornDistance = distance;
        beetleHornDuration = Mathf.Max(0.01f, duration);
        beetleHornTimer = beetleHornDuration;
        beetleHornStartPosition = transform.position;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector3.zero;

        Debug.Log($"[PredictedPlayerMovement] Beetle Horn Impale started. dir={beetleHornDirection}");
    }

    public void StopBeetleHornImpale()
    {
        beetleHornImpaleActive = false;
        beetleHornTimer = 0f;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector3.zero;

        Debug.Log("[PredictedPlayerMovement] Beetle Horn Impale stopped.");
    }

    public void StartBeetleRoll(Vector3 direction, float speed, float duration)
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        beetleRollActive = true;
        beetleRollDirection = direction.normalized;
        beetleRollSpeed = speed;
        beetleRollDuration = Mathf.Max(0.01f, duration);
        beetleRollTimer = beetleRollDuration;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector3.zero;

        Debug.Log($"[PredictedPlayerMovement] Beetle Roll started. dir={beetleRollDirection}, speed={beetleRollSpeed}, duration={beetleRollDuration}");
    }

    public void StopBeetleRoll()
    {
        beetleRollActive = false;
        beetleRollTimer = 0f;

        if (_rigidbody != null)
            _rigidbody.linearVelocity = Vector3.zero;

        Debug.Log("[PredictedPlayerMovement] Beetle Roll stopped.");
    }

    //move handling

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
        public bool isGrounded;
        public void Dispose() {}
    }
}