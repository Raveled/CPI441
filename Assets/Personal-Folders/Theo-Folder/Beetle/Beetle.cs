using UnityEngine;
using System.Collections;

public class Beetle : MonoBehaviour
{
    [Header("Entity Reference")]
    public Entity entity;

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
    private float originalMoveSpeed;
    private bool isSwaggerActive = false;

    [Header("Roll - Ability 3")]
    [SerializeField] private int rollDamage = 15;
    [SerializeField] private float rollSpeed = 15f;
    [SerializeField] private float rollKnockbackForce = 8f;
    [SerializeField] private float rollCooldown = 12f;
    private float rollCooldownTimer = 0f;
    private bool isRolling = false;
    private Rigidbody rollRB;

    [Header("Ground Stomp - Ultimate")]
    [SerializeField] private GameObject stompPrefab;
    [SerializeField] private float stompRadius = 4f;
    [SerializeField] private int stompDamage = 8;
    [SerializeField] private float stompStunDuration = 1.5f;

    private MeshRenderer meshRenderer;
    private Color originalColor;
    private Rigidbody beetleRB;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        beetleRB = GetComponent<Rigidbody>();
        rollRB = beetleRB;

        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;
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
            if (swaggerTimer <= 0f)
                EndSwagger();
        }

        // Roll Update
        if (isRolling)
        {
            HandleRoll();
        }
    }

    #region Basic Attack - Mandible Attack (MELEE)
    public void MandibleAttack()
    {
        if (mandibleCooldownTimer > 0f /*|| entity == null*/) return;

        Entity shooter = entity ?? GetComponent<Entity>();
        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * mandibleRange * 0.5f, mandibleRange);

        foreach (var hit in hits)
        {
            Entity target = hit.GetComponent<Entity>();
            if (target != null && target != shooter && (shooter == null || target.GetTeam() != shooter.GetTeam()))
            {
                target.TakeDamage(mandibleBaseDamage, shooter);
            }
        }

        mandibleCooldownTimer = mandibleCooldown;
        Debug.Log("Mandible Attack! - Beetle.cs");
    }
    #endregion

    #region Ability 1 - Horn Impale (DASH + MELEE)
    public void HornImpale()
    {
        if (hornCooldownTimer > 0f /*|| entity == null*/) return;

        StartCoroutine(HornImpaleRoutine());
    }

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
            if (target != null && target != shooter && (shooter == null || target.GetTeam() != shooter.GetTeam()))
            {
                target.TakeDamage(hornImpaleDamage, shooter);
                // Apply slow (direct moveSpeed modification)
                target.moveSpeed *= (1f - hornSlowAmount);
                StartCoroutine(RestoreTargetSpeed(target, hornSlowDuration));
            }
        }

        hornCooldownTimer = hornCooldown;
        Debug.Log("Horn Impale! - Beetle.cs");
    }

    private IEnumerator RestoreTargetSpeed(Entity target, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (target != null)
            target.moveSpeed *= (1f / (1f - hornSlowAmount));
    }
    #endregion

    #region Ability 2 - Swagger (DEFENSIVE BUFF)
    public void ActivateSwagger()
    {
        if (swaggerTimer > 0f /*|| entity == null*/) return;

        originalMoveSpeed = entity.moveSpeed;
        entity.moveSpeed *= swaggerMoveSpeedMult;

        // Damage reduction (reduce incoming damage)
        StartCoroutine(SwaggerDamageReduction());

        swaggerTimer = swaggerDuration;
        isSwaggerActive = true;

        if (meshRenderer != null)
            meshRenderer.material.color = Color.yellow;

        Debug.Log("Swagger ACTIVATED! - Beetle.cs");
    }

    private IEnumerator SwaggerDamageReduction()
    {
        float originalMaxHP = entity.maximumHitPoints;
        while (swaggerTimer > 0f)
        {
            entity.maximumHitPoints = Mathf.RoundToInt(originalMaxHP * (1f - swaggerDamageReduction));
            yield return null;
        }
        entity.maximumHitPoints = Mathf.RoundToInt(originalMaxHP);
    }

    private void EndSwagger()
    {
        isSwaggerActive = false;
        if (entity != null)
            entity.moveSpeed = originalMoveSpeed;
        if (meshRenderer != null)
            meshRenderer.material.color = originalColor;
        Debug.Log("Swagger ENDED! - Beetle.cs");
    }
    #endregion

    #region Ability 3 - Roll (DASH + DAMAGE + KNOCKBACK)
    public void StartRoll()
    {
        if (rollCooldownTimer > 0f || isRolling /*|| entity == null*/) return;

        Entity shooter = entity ?? GetComponent<Entity>();
        isRolling = true;
        rollCooldownTimer = rollCooldown;

        // CC Immunity (prevent movement interruption)
        StartCoroutine(GrantCCImmunity(3f));

        Debug.Log("Roll STARTED! - Beetle.cs");
    }

    private void HandleRoll()
    {
        if (beetleRB != null)
        {
            beetleRB.linearVelocity = transform.forward * rollSpeed;

            // Check for wall/player collision
            if (Physics.Raycast(transform.position, transform.forward, 0.5f))
            {
                EndRoll();
            }
        }
    }

    private void EndRoll()
    {
        isRolling = false;
        if (beetleRB != null)
            beetleRB.linearVelocity = Vector3.zero;
        Debug.Log("Roll ENDED! - Beetle.cs");
    }

    private IEnumerator GrantCCImmunity(float duration)
    {
        // Prevent movement interruption during roll
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    #endregion

    #region Ultimate - Ground Stomp (AOE STUN)
    public void GroundStomp()
    {
        if (stompPrefab == null /*|| entity == null*/) return;

        Entity shooter = entity ?? GetComponent<Entity>();

        // Visual effect
        Instantiate(stompPrefab, transform.position, Quaternion.identity);

        // AOE damage + stun
        Collider[] hits = Physics.OverlapSphere(transform.position, stompRadius);
        foreach (var hit in hits)
        {
            Entity target = hit.GetComponent<Entity>();
            if (target != null && target != shooter && (shooter == null || target.GetTeam() != shooter.GetTeam()))
            {
                target.TakeDamage(stompDamage, shooter);
                // Stun (set moveSpeed to 0 temporarily)
                StartCoroutine(StunTarget(target, stompStunDuration));
            }
        }

        Debug.Log("GROUND STOMP! - Beetle.cs");
    }

    private IEnumerator StunTarget(Entity target, float duration)
    {
        float originalSpeed = target.moveSpeed;
        target.moveSpeed = 0f;
        yield return new WaitForSeconds(duration);
        if (target != null)
            target.moveSpeed = originalSpeed;
    }
    #endregion

    // Multipliers for movement scripts
    public float GetMoveSpeedMultiplier() => isSwaggerActive ? swaggerMoveSpeedMult : 1f;
}
