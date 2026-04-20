// Beetle.cs - FULLY ADAPTED TO MOSQUITO STRUCTURE
using PurrNet;
using System.Collections;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class Beetle : NetworkBehaviour
{
    [SerializeField] public Player player;

    [Header("Basic Attack - Mandible Attack")]
    [SerializeField] private int mandibleBaseDamage = 12;
    [SerializeField] private float mandibleRange = 2.5f;
    [SerializeField] private float mandibleCooldown = 1.2f;
    private float mandibleCooldownTimer = 0f;

    [Header("Horn Impale - Ability 1")]
    [SerializeField] private int hornImpaleDamage = 25;
    [SerializeField] private float hornDashDistance = 4f;
    [SerializeField] private float hornDashDuration = 0.4f;
    [SerializeField] private float hornSlowAmount = 0.5f;
    [SerializeField] private float hornSlowDuration = 2f;
    [SerializeField] private float hornCooldown = 8f;
    private float hornCooldownTimer = 0f;

    [Header("Swagger - Ability 2")]
    [SerializeField] private float swaggerMoveSpeedMult = 0.6f;
    [SerializeField] private float swaggerDamageReduction = 0.4f;
    [SerializeField] private float swaggerDuration = 6f;
    private float swaggerTimer = 0f;
    private bool isSwaggerActive = false;

    [Header("Roll - Ability 3")]
    [SerializeField] private int rollDamage = 15;
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float rollKnockbackForce = 8f;
    [SerializeField] private float rollCooldown = 12f;
    private float rollCooldownTimer = 0f;
    private bool isRolling = false;

    [Header("Ground Stomp - Ultimate")]
    [SerializeField] private GameObject stompPrefab;
    [SerializeField] private float stompRadius = 4f;
    [SerializeField] private int stompDamage = 8;
    [SerializeField] private float stompStunDuration = 1.5f;

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
    [SerializeField] private float rollDuration = 1.0f;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        beetleRB = GetComponentInParent<Rigidbody>();
        predictedMovement = GetComponentInParent<PredictedPlayerMovement>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
        //if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned();
        Debug.Log($"Beetle OnSpawned {gameObject.name} isOwner:{isOwner} isController:{isController} isServer:{isServer}");
        player = GetComponent<Player>();
        if (player == null)
            player = GetComponentInParent<Player>();

        predictedMovement = GetComponentInParent<PredictedPlayerMovement>();

        BeetleInputTester inputTester = GetComponent<BeetleInputTester>();
        if (inputTester != null) inputTester.EnableInput();

        StartCoroutine(DelayedSpawn(asServer));
    }
    private IEnumerator DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(0.05f);

        base.OnSpawned();

        GameObject parentObject = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (animator == null)
            animator = parentObject.GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Cooldowns
        if (mandibleCooldownTimer > 0f) mandibleCooldownTimer -= Time.deltaTime;
        if (hornCooldownTimer > 0f) hornCooldownTimer -= Time.deltaTime;
        if (rollCooldownTimer > 0f) rollCooldownTimer -= Time.deltaTime;

        // Swagger Update
        if (swaggerTimer > 0f)
        {
            swaggerTimer -= Time.deltaTime;
            if (swaggerTimer <= 0f) EndSwagger();
        }
    }

    // BASIC ATTACK - MANDIBLE ATTACK
    public void CastMandibleAttack()
    {
        if (!isController) return;
        Debug.Log($"Beetle CastMandibleAttack on {gameObject.name} isController:{isController}");

        PlayMandibleAnimServerRpc();
        mandibleCooldownTimer = mandibleCooldown;

        if (isServer) ApplyMandibleAttack();
        else ApplyMandibleAttackServerRpc();
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
    }

    // ABILITY 1 - HORN IMPALE
    public bool TryHornImpale()
    {
        if (!isController) return false;
        if (hornCooldownTimer > 0f || player == null || isHornImpaling)
        {
            Debug.Log($"Horn Impale blocked - cooldown {hornCooldownTimer:F2}s remaining or already active");
            return false;
        }

        Vector3 dashDirection = transform.forward;
        if (dashDirection.sqrMagnitude <= 0.001f)
            dashDirection = Vector3.forward;

        PlayHornImpaleAnimServerRpc();
        ServerStartHornImpaleRpc(dashDirection);

        return true;
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
        if (hornCooldownTimer > 0f || isHornImpaling) return;

        Vector3 finalDirection = direction.normalized;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = transform.forward;

        hornCooldownTimer = hornCooldown;
        isHornImpaling = true;
        currentHornDirection = finalDirection;

        BeginHornImpaleObserversRpc(finalDirection);

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
        if (swaggerTimer > 0f)
        {
            Debug.Log("Swagger blocked - already active.");
            return;
        }

        PlaySwaggerAnimServerRpc();
        swaggerTimer = swaggerDuration;
        isSwaggerActive = true;

        if (meshRenderer != null) meshRenderer.material.color = Color.yellow;

        if (isServer) ApplySwagger();
        else ApplySwaggerServerRpc();

        Debug.Log("Swagger ACTIVATED! - Beetle.cs");
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
        if (player != null)
        {
            player.ModifyMoveSpeedMultiplier(swaggerMoveSpeedMult, swaggerDuration);
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
        if (rollCooldownTimer > 0f || isRolling || player == null)
        {
            Debug.Log("Roll blocked - cooldown or already rolling");
            return false;
        }

        Vector3 finalDirection = transform.forward;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = Vector3.forward;

        PlayRollAnimServerRpc();
        ServerStartRollRpc(finalDirection);

        Debug.Log("Roll requested! - Beetle.cs");
        return true;
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
        if (rollCooldownTimer > 0f || isRolling) return;

        Vector3 finalDirection = direction.normalized;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = transform.forward;

        rollCooldownTimer = rollCooldown;
        isRolling = true;
        currentRollDirection = finalDirection;

        BeginRollObserversRpc(finalDirection);

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
        if (stompPrefab == null || player == null) return;

        PlayStompAnimServerRpc();

        if (isServer) ApplyGroundStomp();
        else GroundStompServerRpc();
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
        // Visual effect
        if (stompPrefab != null)
        {
            GameObject stompGO = Instantiate(stompPrefab, transform.position, Quaternion.identity);
            NetworkManager.main.Spawn(stompGO);
        }

        // AOE damage + stun
        Collider[] hits = Physics.OverlapSphere(transform.position, stompRadius);
        foreach (var hit in hits)
        {
            Player target = hit.GetComponent<Player>();
            if (target != null && target != player && !player.GetEnemyTeams().Contains(target.GetTeam()))
            {
                target.TakeDamage(stompDamage, player);
                target.ModifyMoveSpeedMultiplier(0f, stompStunDuration);
            }
        }
        Debug.Log("GROUND STOMP! - Beetle.cs");
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
