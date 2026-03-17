// Beetle.cs - FULLY ADAPTED TO MOSQUITO STRUCTURE
using UnityEngine;
using System.Collections;
using PurrNet;

public class Beetle : NetworkBehaviour
{
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

    [Header("Entity Reference")]
    public Entity entity;

    private MeshRenderer meshRenderer;
    private Color originalColor;
    private Rigidbody beetleRB;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        beetleRB = GetComponent<Rigidbody>();
        if (meshRenderer != null) originalColor = meshRenderer.material.color;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        Debug.Log($"Beetle OnSpawned {gameObject.name} isOwner:{isOwner} isController:{isController} isServer:{isServer}");
        entity = GetComponent<Entity>();
        BeetleInputTester inputTester = GetComponent<BeetleInputTester>();
        if (inputTester != null) inputTester.EnableInput();
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

        // Roll Update
        if (isRolling) HandleRoll();
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
        if (animator != null) animator.SetTrigger("Mandible");
    }

    [ServerRpc]
    private void ApplyMandibleAttackServerRpc() => ApplyMandibleAttack();

    private void ApplyMandibleAttack()
    {
        if (entity == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * mandibleRange, mandibleRange);
        foreach (var hit in hits)
        {
            Entity target = hit.GetComponent<Entity>();
            if (target != null && target != entity && !entity.GetEnemyTeams().Contains(target.GetTeam()))
            {
                target.TakeDamage(mandibleBaseDamage, entity);
            }
        }
        Debug.Log("Mandible Attack! - Beetle.cs");
    }

    // ABILITY 1 - HORN IMPALE
    public bool TryHornImpale()
    {
        if (!isController) return false;
        if (hornCooldownTimer > 0f || entity == null)
        {
            Debug.Log($"Horn Impale blocked - cooldown {hornCooldownTimer:F2}s remaining");
            return false;
        }

        PlayHornImpaleAnimServerRpc();
        hornCooldownTimer = hornCooldown;

        if (isServer) StartCoroutine(HornImpaleRoutine());
        else HornImpaleServerRpc();

        return true;
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayHornImpaleAnimServerRpc() => PlayHornImpaleAnim();

    [ObserversRpc]
    private void PlayHornImpaleAnim()
    {
        if (animator != null) animator.SetTrigger("HornImpale");
    }

    [ServerRpc]
    private void HornImpaleServerRpc() => StartCoroutine(HornImpaleRoutine());

    private IEnumerator HornImpaleRoutine()
    {
        Entity shooter = entity ?? GetComponent<Entity>();
        Vector3 dashEnd = transform.position + transform.forward * hornDashDistance;
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < hornDashDuration)
        {
            transform.position = Vector3.Lerp(startPos, dashEnd, elapsed / hornDashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Damage at end position
        Collider[] hits = Physics.OverlapSphere(transform.position, 1.5f);
        foreach (var hit in hits)
        {
            Entity target = hit.GetComponent<Entity>();
            if (target != null && target != shooter && shooter != null && !shooter.GetEnemyTeams().Contains(target.GetTeam()))
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
        if (entity != null)
        {
            entity.ModifyMoveSpeedMultiplier(swaggerMoveSpeedMult, swaggerDuration);
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
        if (rollCooldownTimer > 0f || isRolling || entity == null)
        {
            Debug.Log("Roll blocked - cooldown or already rolling");
            return false;
        }

        PlayRollAnimServerRpc();
        rollCooldownTimer = rollCooldown;
        isRolling = true;

        if (isServer) StartCoroutine(GrantCCImmunity(3f));

        Debug.Log("Roll STARTED! - Beetle.cs");
        return true;
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayRollAnimServerRpc() => PlayRollAnim();

    [ObserversRpc]
    private void PlayRollAnim()
    {
        if (animator != null) animator.SetTrigger("Roll");
    }

    private void HandleRoll()
    {
        if (beetleRB != null)
        {
            beetleRB.linearVelocity = transform.forward * rollSpeed;
        }

        if (Physics.Raycast(transform.position, transform.forward, 0.5f))
        {
            EndRoll();
        }
    }

    private void EndRoll()
    {
        isRolling = false;
        if (beetleRB != null) beetleRB.linearVelocity = Vector3.zero;
        Debug.Log("Roll ENDED! - Beetle.cs");
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
        if (stompPrefab == null || entity == null) return;

        PlayStompAnimServerRpc();

        if (isServer) ApplyGroundStomp();
        else GroundStompServerRpc();
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayStompAnimServerRpc() => PlayStompAnim();

    [ObserversRpc]
    private void PlayStompAnim()
    {
        if (animator != null) animator.SetTrigger("Stomp");
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
            Entity target = hit.GetComponent<Entity>();
            if (target != null && target != entity && !entity.GetEnemyTeams().Contains(target.GetTeam()))
            {
                target.TakeDamage(stompDamage, entity);
                target.ModifyMoveSpeedMultiplier(0f, stompStunDuration);
            }
        }
        Debug.Log("GROUND STOMP! - Beetle.cs");
    }

    public float GetMoveSpeedMultiplier()
    {
        return entity != null ? entity.GetMoveSpeed() : 1f;
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
