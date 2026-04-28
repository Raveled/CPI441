// Beetle.cs - ADAPTED WITH MOSQUITO COOLDOWN BEHAVIOR
using PurrNet;
using System.Collections;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Beetle : NetworkBehaviour
{
    [SerializeField] public Player player;
    [SerializeField] public BeetleInputTester inputTester;

    [Header("UI cooldown hook up")]
    [SerializeField] private AbilityBarUI abilityBar;

    [Header("Basic Attack - Mandible Attack")]
    [SerializeField] private int mandibleBaseDamage = 12;
    [SerializeField] private float mandibleRange = 2.5f;
    [SerializeField] private float mandibleCooldown = 1.2f;
    [SerializeField] private int mandibleAbilityBarIndex = 0;
    [SerializeField] private bool startMandibleCooldownLocallyOnInput = true;
    [SerializeField] private bool enableMandibleDebugLogs = true;

    [Header("Horn Impale - Ability 1")]
    [SerializeField] private int hornImpaleDamage = 25;
    [SerializeField] private float hornDashDistance = 4f;
    [SerializeField] private float hornDashDuration = 0.4f;
    [SerializeField] private float hornSlowAmount = 0.5f;
    [SerializeField] private float hornSlowDuration = 2f;
    [SerializeField] private float hornCooldown = 8f;
    [SerializeField] private int hornImpaleAbilityBarIndex = 0;
    [SerializeField] private bool startHornImpaleCooldownLocallyOnInput = true;
    [SerializeField] private bool enableHornImpaleDebugLogs = true;

    [Header("Swagger - Ability 2")]
    [SerializeField] private float swaggerMoveSpeedMult = 0.6f;
    [SerializeField] private float swaggerDamageReduction = 0.4f;
    [SerializeField] private float swaggerDuration = 6f;
    [SerializeField] private float swaggerCooldown = 12f;
    [SerializeField] private int swaggerAbilityBarIndex = 1;
    [SerializeField] private bool startSwaggerCooldownLocallyOnInput = true;
    [SerializeField] private bool enableSwaggerDebugLogs = true;
    private float swaggerTimer = 0f;
    private bool isSwaggerActive = false;

    [Header("Roll - Ability 3")]
    [SerializeField] private int rollDamage = 15;
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float rollKnockbackForce = 8f;
    [SerializeField] private float rollCooldown = 12f;
    [SerializeField] private float rollDuration = 1.0f;
    [SerializeField] private int rollAbilityBarIndex = 2;
    [SerializeField] private bool startRollCooldownLocallyOnInput = true;
    [SerializeField] private bool enableRollDebugLogs = true;

    [Header("Ground Stomp - Ultimate")]
    [SerializeField] private GameObject stompPrefab;
    [SerializeField] private float stompRadius = 4f;
    [SerializeField] private int stompDamage = 8;
    [SerializeField] private float stompDuration = 3f;
    [SerializeField] private float stompTickInterval = 0.5f;
    [SerializeField] private float stompStunDuration = 1.5f;
    [SerializeField] private float stompCooldown = 20f;
    [SerializeField] private int stompAbilityBarIndex = 3;
    [SerializeField] private bool startStompCooldownLocallyOnInput = true;
    [SerializeField] private bool enableStompDebugLogs = true;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("player Reference")]

    private MeshRenderer meshRenderer;
    private Color originalColor;
    private Rigidbody beetleRB;

    private PredictedPlayerMovement predictedMovement;

    private bool isHornImpaling = false;
    private Vector3 currentHornDirection = Vector3.forward;

    private Vector3 currentRollDirection = Vector3.forward;
    private bool isMovingAnimState = false;
    private bool isRolling = false;

    // Local cooldown timers
    private float mandibleCooldownTimer = 0f;
    private float hornCooldownTimer = 0f;
    private float swaggerCooldownTimer = 0f;
    private float rollCooldownTimer = 0f;
    private float stompCooldownTimer = 0f;

    // Server authoritative cooldown times
    private float mandibleNextAllowedTimeServer = 0f;
    private float hornImpaleNextAllowedTimeServer = 0f;
    private float swaggerNextAllowedTimeServer = 0f;
    private float rollNextAllowedTimeServer = 0f;
    private float stompNextAllowedTimeServer = 0f;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        beetleRB = GetComponentInParent<Rigidbody>();
        predictedMovement = GetComponentInParent<PredictedPlayerMovement>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
    }

    protected override void OnSpawned(bool asServer)
    {
        StartCoroutine(DelayedSpawn(asServer));
    }

    private IEnumerator DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(0.05f);

        base.OnSpawned();

        Debug.Log($"Beetle OnSpawned {gameObject.name} isOwner:{isOwner} isController:{isController} isServer:{isServer}");
        if (player == null)
            player = GetComponentInParent<Player>();

        predictedMovement = GetComponentInParent<PredictedPlayerMovement>();

        if (inputTester == null)
        {
            inputTester = GetComponent<BeetleInputTester>();
        }
        inputTester.EnableInput();

        GameObject parentObject = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (animator == null)
            animator = parentObject.GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Update local cooldown timers
        if (mandibleCooldownTimer > 0f)
            mandibleCooldownTimer -= Time.deltaTime;

        if (hornCooldownTimer > 0f)
            hornCooldownTimer -= Time.deltaTime;

        if (swaggerCooldownTimer > 0f)
            swaggerCooldownTimer -= Time.deltaTime;

        if (rollCooldownTimer > 0f)
            rollCooldownTimer -= Time.deltaTime;

        if (stompCooldownTimer > 0f)
            stompCooldownTimer -= Time.deltaTime;

        if (abilityBar == null && player != null && player.isLocalPlayer())
            abilityBar = FindFirstObjectByType<AbilityBarUI>();

        // Swagger Update
        if (swaggerTimer > 0f)
        {
            swaggerTimer -= Time.deltaTime;
            if (swaggerTimer <= 0f) EndSwagger();
        }

        UpdateIsMovingAnimation();
    }

    private void UpdateAbilityBarCooldown(int index, float cooldown)
    {
        if (abilityBar == null || player == null || !player.isLocalPlayer())
            return;

        if (index < 0)
            return;

        abilityBar.UseAbility(index, cooldown);
    }

    private void UpdateIsMovingAnimation()
    {
        if (!isServer) return;
        if (animator == null || predictedMovement == null || predictedMovement._rigidbody == null) return;

        Vector3 velocity = predictedMovement._rigidbody.linearVelocity;
        velocity.y = 0f;

        bool isWalking = velocity.sqrMagnitude > 0.05f;
        bool shouldBeMoving = isWalking || isHornImpaling || isRolling;

        if (shouldBeMoving == isMovingAnimState) return;

        isMovingAnimState = shouldBeMoving;
        SetIsMovingObserversRpc(shouldBeMoving);
    }

    [ObserversRpc]
    private void SetIsMovingObserversRpc(bool moving)
    {
        isMovingAnimState = moving;

        if (animator != null)
            animator.SetBool("IsMoving", moving);
    }

    // BASIC ATTACK - MANDIBLE ATTACK
    public void CastMandibleAttack()
    {
        if (!isController) return;

        if (mandibleCooldownTimer > 0f)
        {
            if (enableMandibleDebugLogs)
            {
                Debug.Log($"[Beetle] Mandible Attack blocked locally | remaining={Mathf.Max(0f, mandibleCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        Debug.Log($"Beetle CastMandibleAttack on {gameObject.name} isController:{isController}");

        if (startMandibleCooldownLocallyOnInput)
            StartMandibleCooldownClient(mandibleCooldown);

        PlayMandibleAnimServerRpc();

        if (isServer) ApplyMandibleAttack();
        else ApplyMandibleAttackServerRpc();
    }

    private void StartMandibleCooldownClient(float cooldown)
    {
        mandibleCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(mandibleAbilityBarIndex, cooldown);

        if (enableMandibleDebugLogs)
        {
            Debug.Log($"[Beetle] Mandible Attack local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayMandibleAnimServerRpc() => PlayMandibleAnim();

    [ObserversRpc]
    private void PlayMandibleAnim()
    {
        if (animator != null) animator.SetTrigger("MandibleAttack");
    }

    [ServerRpc]
    private void ApplyMandibleAttackServerRpc() => ApplyMandibleAttack();

    private void ApplyMandibleAttack()
    {
        if (player == null) return;

        float serverTime = Time.time;
        float remaining = mandibleNextAllowedTimeServer - serverTime;

        if (enableMandibleDebugLogs)
        {
            Debug.Log($"[Beetle] Mandible Attack on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={mandibleNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < mandibleNextAllowedTimeServer)
        {
            RejectMandibleCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        mandibleNextAllowedTimeServer = serverTime + mandibleCooldown;

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * mandibleRange, mandibleRange);
        foreach (var hit in hits)
        {
            Player target = hit.GetComponent<Player>();
            if (target != null && target != player && !player.GetEnemyTeams().Contains(target.GetTeam()))
            {
                target.TakeDamage(mandibleBaseDamage, player);
            }
        }
        Debug.Log("Mandible Attack! - Beetle.cs");

        SyncMandibleCooldownClientRpc(mandibleCooldown);
    }

    [ObserversRpc]
    private void SyncMandibleCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        mandibleCooldownTimer = Mathf.Max(mandibleCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(mandibleAbilityBarIndex, cooldown);

        if (enableMandibleDebugLogs)
        {
            Debug.Log($"[Beetle] Mandible Attack cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectMandibleCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        mandibleCooldownTimer = Mathf.Max(mandibleCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(mandibleAbilityBarIndex, remainingCooldown);

        if (enableMandibleDebugLogs)
        {
            Debug.Log($"[Beetle] Mandible Attack rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    // ABILITY 1 - HORN IMPALE
    public bool TryHornImpale()
    {
        if (!isController) return false;

        if (hornCooldownTimer > 0f)
        {
            if (enableHornImpaleDebugLogs)
            {
                Debug.Log($"[Beetle] Horn Impale blocked locally | remaining={Mathf.Max(0f, hornCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return false;
        }

        if (player == null || isHornImpaling)
        {
            Debug.Log($"Horn Impale blocked - player null or already active");
            return false;
        }

        Vector3 dashDirection = transform.forward;
        if (dashDirection.sqrMagnitude <= 0.001f)
            dashDirection = Vector3.forward;

        if (enableHornImpaleDebugLogs)
        {
            Debug.Log($"[Beetle] Horn Impale requested locally | playerId={player.GetPlayerID()} | localTimer={hornCooldownTimer:F2}s");
        }

        if (startHornImpaleCooldownLocallyOnInput)
            StartHornImpaleCooldownClient(hornCooldown);

        PlayHornImpaleAnimServerRpc();
        ServerStartHornImpaleRpc(dashDirection);

        return true;
    }

    private void StartHornImpaleCooldownClient(float cooldown)
    {
        hornCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(hornImpaleAbilityBarIndex, cooldown);

        if (enableHornImpaleDebugLogs)
        {
            Debug.Log($"[Beetle] Horn Impale local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayHornImpaleAnimServerRpc() => PlayHornImpaleAnim();

    [ObserversRpc]
    private void PlayHornImpaleAnim()
    {
        if (animator != null) animator.SetTrigger("HornImpaleAttack");
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerStartHornImpaleRpc(Vector3 direction)
    {
        if (!isServer) return;
        if (player == null) return;

        float serverTime = Time.time;
        float remaining = hornImpaleNextAllowedTimeServer - serverTime;

        if (enableHornImpaleDebugLogs)
        {
            Debug.Log($"[Beetle] Horn Impale request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={hornImpaleNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < hornImpaleNextAllowedTimeServer)
        {
            RejectHornImpaleCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        if (hornCooldownTimer > 0f || isHornImpaling) return;

        Vector3 finalDirection = direction.normalized;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = transform.forward;

        hornImpaleNextAllowedTimeServer = serverTime + hornCooldown;
        hornCooldownTimer = hornCooldown;
        isHornImpaling = true;
        currentHornDirection = finalDirection;

        BeginHornImpaleObserversRpc(finalDirection);
        SyncHornImpaleCooldownClientRpc(hornCooldown);

        if (predictedMovement != null)
            predictedMovement.StartBeetleHornImpale(finalDirection, hornDashDistance, hornDashDuration);

        Debug.Log($"[Beetle] Horn Impale started. dir={finalDirection}");
    }

    [ObserversRpc]
    private void BeginHornImpaleObserversRpc(Vector3 direction)
    {
        isHornImpaling = true;
        currentHornDirection = direction.normalized;

        if (currentHornDirection.sqrMagnitude <= 0.001f)
            currentHornDirection = transform.forward;

        if (!isServer && predictedMovement != null)
            predictedMovement.StartBeetleHornImpale(currentHornDirection, hornDashDistance, hornDashDuration);
    }

    [ObserversRpc]
    private void SyncHornImpaleCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        hornCooldownTimer = Mathf.Max(hornCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(hornImpaleAbilityBarIndex, cooldown);

        if (enableHornImpaleDebugLogs)
        {
            Debug.Log($"[Beetle] Horn Impale cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectHornImpaleCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        hornCooldownTimer = Mathf.Max(hornCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(hornImpaleAbilityBarIndex, remainingCooldown);

        if (enableHornImpaleDebugLogs)
        {
            Debug.Log($"[Beetle] Horn Impale rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    public void NotifyHornImpaleEndedFromMovement()
    {
        if (!isServer) return;
        if (!isHornImpaling) return;

        ApplyHornImpaleHit();
        EndHornImpaleServer();
    }

    private void EndHornImpaleServer()
    {
        isHornImpaling = false;
        EndHornImpaleObserversRpc();
    }

    [ObserversRpc]
    private void EndHornImpaleObserversRpc()
    {
        isHornImpaling = false;

        if (predictedMovement != null)
            predictedMovement.StopBeetleHornImpale();

        Debug.Log("[Beetle] Horn Impale ended.");
    }

    private void ApplyHornImpaleHit()
    {
        Entity shooter = player;
        if (shooter == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            Entity target = hit.GetComponent<Entity>();
            if (target != null && target != shooter && !shooter.GetEnemyTeams().Contains(target.GetTeam()))
            {
                target.TakeDamage(hornImpaleDamage, shooter);
                target.ModifyMoveSpeedMultiplier(1f - hornSlowAmount, hornSlowDuration);
            }
        }

        Debug.Log("Horn Impale! - Beetle.cs");
    }

    // ABILITY 2 - SWAGGER
    public void ActivateSwagger()
    {
        if (!isController) return;

        if (swaggerCooldownTimer > 0f)
        {
            if (enableSwaggerDebugLogs)
            {
                Debug.Log($"[Beetle] Swagger blocked locally by cooldown | remaining={Mathf.Max(0f, swaggerCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (swaggerTimer > 0f)
        {
            Debug.Log("Swagger blocked - already active.");
            return;
        }

        if (enableSwaggerDebugLogs)
        {
            Debug.Log($"[Beetle] Swagger requested locally | playerId={player.GetPlayerID()} | localTimer={swaggerCooldownTimer:F2}s");
        }

        if (startSwaggerCooldownLocallyOnInput)
            StartSwaggerCooldownClient(swaggerCooldown);

        PlaySwaggerAnimServerRpc();

        if (isServer) ApplySwagger();
        else ApplySwaggerServerRpc();
    }

    private void StartSwaggerCooldownClient(float cooldown)
    {
        swaggerCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(swaggerAbilityBarIndex, cooldown);

        if (enableSwaggerDebugLogs)
        {
            Debug.Log($"[Beetle] Swagger local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void PlaySwaggerAnimServerRpc() => PlaySwaggerAnim();

    [ObserversRpc]
    private void PlaySwaggerAnim()
    {
        if (animator != null) animator.SetTrigger("Swagger");
    }

    [ServerRpc]
    private void ApplySwaggerServerRpc() => ApplySwagger();

    private void ApplySwagger()
    {
        if (player == null) return;

        float serverTime = Time.time;
        float remaining = swaggerNextAllowedTimeServer - serverTime;

        if (enableSwaggerDebugLogs)
        {
            Debug.Log($"[Beetle] Swagger request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={swaggerNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < swaggerNextAllowedTimeServer)
        {
            RejectSwaggerCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        if (swaggerTimer > 0f)
        {
            Debug.Log("[Beetle] Swagger blocked on server - already active.");
            return;
        }

        swaggerNextAllowedTimeServer = serverTime + swaggerCooldown;
        swaggerTimer = swaggerDuration;
        isSwaggerActive = true;

        if (meshRenderer != null) meshRenderer.material.color = Color.yellow;

        player.ModifyMoveSpeedMultiplier(swaggerMoveSpeedMult, swaggerDuration);

        SyncSwaggerCooldownClientRpc(swaggerCooldown);

        Debug.Log("Swagger ACTIVATED! - Beetle.cs");
    }

    [ObserversRpc]
    private void SyncSwaggerCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        swaggerCooldownTimer = Mathf.Max(swaggerCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(swaggerAbilityBarIndex, cooldown);

        if (enableSwaggerDebugLogs)
        {
            Debug.Log($"[Beetle] Swagger cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectSwaggerCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        swaggerCooldownTimer = Mathf.Max(swaggerCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(swaggerAbilityBarIndex, remainingCooldown);

        if (enableSwaggerDebugLogs)
        {
            Debug.Log($"[Beetle] Swagger rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void EndSwagger()
    {
        isSwaggerActive = false;
        if (meshRenderer != null) meshRenderer.material.color = originalColor;
        Debug.Log("Swagger ENDED! - Beetle.cs");
    }

    // ABILITY 3 - ROLL
    public bool TryRoll()
    {
        if (!isController) return false;

        if (rollCooldownTimer > 0f)
        {
            if (enableRollDebugLogs)
            {
                Debug.Log($"[Beetle] Roll blocked locally | remaining={Mathf.Max(0f, rollCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return false;
        }

        if (isRolling || player == null)
        {
            Debug.Log("Roll blocked - already rolling");
            return false;
        }

        Vector3 finalDirection = transform.forward;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = Vector3.forward;

        if (enableRollDebugLogs)
        {
            Debug.Log($"[Beetle] Roll requested locally | playerId={player.GetPlayerID()} | localTimer={rollCooldownTimer:F2}s");
        }

        if (startRollCooldownLocallyOnInput)
            StartRollCooldownClient(rollCooldown);

        PlayRollAnimServerRpc();
        ServerStartRollRpc(finalDirection);

        Debug.Log("Roll requested! - Beetle.cs");
        return true;
    }

    private void StartRollCooldownClient(float cooldown)
    {
        rollCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(rollAbilityBarIndex, cooldown);

        if (enableRollDebugLogs)
        {
            Debug.Log($"[Beetle] Roll local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayRollAnimServerRpc() => PlayRollAnim();

    [ObserversRpc]
    private void PlayRollAnim()
    {
        if (animator != null) animator.SetTrigger("Roll");
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerStartRollRpc(Vector3 direction)
    {
        if (!isServer) return;
        if (player == null) return;

        float serverTime = Time.time;
        float remaining = rollNextAllowedTimeServer - serverTime;

        if (enableRollDebugLogs)
        {
            Debug.Log($"[Beetle] Roll request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={rollNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < rollNextAllowedTimeServer)
        {
            RejectRollCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        if (rollCooldownTimer > 0f || isRolling) return;

        Vector3 finalDirection = direction.normalized;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = transform.forward;

        rollNextAllowedTimeServer = serverTime + rollCooldown;
        rollCooldownTimer = rollCooldown;
        isRolling = true;
        currentRollDirection = finalDirection;

        BeginRollObserversRpc(finalDirection);
        SyncRollCooldownClientRpc(rollCooldown);

        if (predictedMovement != null)
            predictedMovement.StartBeetleRoll(finalDirection, rollSpeed, rollDuration);

        StartCoroutine(GrantCCImmunity(rollDuration));

        Debug.Log($"[Beetle] Roll started. dir={finalDirection}, speed={rollSpeed}, duration={rollDuration}");
    }

    [ObserversRpc]
    private void BeginRollObserversRpc(Vector3 direction)
    {
        isRolling = true;
        currentRollDirection = direction.normalized;

        if (currentRollDirection.sqrMagnitude <= 0.001f)
            currentRollDirection = transform.forward;

        if (!isServer && predictedMovement != null)
            predictedMovement.StartBeetleRoll(currentRollDirection, rollSpeed, rollDuration);
    }

    [ObserversRpc]
    private void SyncRollCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        rollCooldownTimer = Mathf.Max(rollCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(rollAbilityBarIndex, cooldown);

        if (enableRollDebugLogs)
        {
            Debug.Log($"[Beetle] Roll cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectRollCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        rollCooldownTimer = Mathf.Max(rollCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(rollAbilityBarIndex, remainingCooldown);

        if (enableRollDebugLogs)
        {
            Debug.Log($"[Beetle] Roll rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    public void NotifyRollEndedFromMovement()
    {
        if (!isServer) return;
        if (!isRolling) return;

        EndRollServer();
    }

    private void EndRollServer()
    {
        isRolling = false;
        EndRollObserversRpc();
    }

    [ObserversRpc]
    private void EndRollObserversRpc()
    {
        isRolling = false;

        if (predictedMovement != null)
            predictedMovement.StopBeetleRoll();

        Debug.Log("[Beetle] Roll ended.");
    }

    private IEnumerator GrantCCImmunity(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ULTIMATE - GROUND STOMP
    public void CastGroundStomp()
    {
        if (!isController) return;

        if (stompCooldownTimer > 0f)
        {
            if (enableStompDebugLogs)
            {
                Debug.Log($"[Beetle] Ground Stomp blocked locally | remaining={Mathf.Max(0f, stompCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableStompDebugLogs)
        {
            Debug.Log($"[Beetle] Ground Stomp requested locally | playerId={player.GetPlayerID()} | localTimer={stompCooldownTimer:F2}s");
        }

        if (startStompCooldownLocallyOnInput)
            StartStompCooldownClient(stompCooldown);

        PlayStompAnimServerRpc();

        if (isServer) ApplyGroundStomp();
        else GroundStompServerRpc();
    }

    private void StartStompCooldownClient(float cooldown)
    {
        stompCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(stompAbilityBarIndex, cooldown);

        if (enableStompDebugLogs)
        {
            Debug.Log($"[Beetle] Ground Stomp local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayStompAnimServerRpc() => PlayStompAnim();

    [ObserversRpc]
    private void PlayStompAnim()
    {
        if (animator != null) animator.SetTrigger("StompAttack");
    }

    [ServerRpc]
    private void GroundStompServerRpc() => ApplyGroundStomp();

    private void ApplyGroundStomp()
    {
        if (stompPrefab == null) { Debug.LogError("[Beetle] stompPrefab is NULL!"); return; }
        if (player == null) { Debug.LogError("[Beetle] player is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Beetle] networkManager is NULL!"); return; }

        float serverTime = Time.time;
        float remaining = stompNextAllowedTimeServer - serverTime;

        if (enableStompDebugLogs)
        {
            Debug.Log($"[Beetle] Ground Stomp request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={stompNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < stompNextAllowedTimeServer)
        {
            RejectStompCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        stompNextAllowedTimeServer = serverTime + stompCooldown;

        GameObject stompGO = Instantiate(stompPrefab, transform.position, Quaternion.identity);

        GroundStompArea stomp = stompGO.GetComponent<GroundStompArea>();
        if (stomp == null)
        {
            Debug.LogError("[Beetle] GroundStompArea component missing from stompPrefab root!");
            Destroy(stompGO);
            return;
        }

        NetworkManager.main.Spawn(stompGO);

        stomp.SpawnSetup(
            player,
            stompRadius,
            stompDuration,
            stompDamage,
            stompTickInterval,
            stompStunDuration
        );

        SyncStompCooldownClientRpc(stompCooldown);

        Debug.Log($"[Beetle] Ground Stomp spawned at {transform.position}");
    }

    [ObserversRpc]
    private void SyncStompCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        stompCooldownTimer = Mathf.Max(stompCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(stompAbilityBarIndex, cooldown);

        if (enableStompDebugLogs)
        {
            Debug.Log($"[Beetle] Ground Stomp cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectStompCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        stompCooldownTimer = Mathf.Max(stompCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(stompAbilityBarIndex, remainingCooldown);

        if (enableStompDebugLogs)
        {
            Debug.Log($"[Beetle] Ground Stomp rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    public float GetMoveSpeedMultiplier()
    {
        return player != null ? player.GetMoveSpeed() : 1f;
    }

    [ContextMenu("Test Mandible Attack")]
    private void TestMandibleAttack() => CastMandibleAttack();

    [ContextMenu("Test Horn Impale")]
    private void TestHornImpale() => TryHornImpale();

    [ContextMenu("Test Swagger")]
    private void TestSwagger() => ActivateSwagger();

    [ContextMenu("Test Roll")]
    private void TestRoll() => TryRoll();

    [ContextMenu("Test Ground Stomp")]
    private void TestGroundStomp() => CastGroundStomp();
}