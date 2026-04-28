using UnityEngine;
using System.Collections;
using PurrNet;

public class GroundStompArea : NetworkBehaviour
{
    [Header("Runtime Setup")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private SphereCollider areaCollider;

    [Header("Owner / Damage")]
    [SerializeField] private NetworkID? ownerId;
    [SerializeField] private float radius = 4f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private int damagePerTick = 8;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float stunDuration = 1.5f;

    private bool initialized;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (areaCollider == null) areaCollider = GetComponent<SphereCollider>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (rb != null)
            rb.isKinematic = true;

        if (areaCollider != null)
        {
            areaCollider.isTrigger = true;
            areaCollider.radius = radius;
        }
    }

    public void SpawnSetup(Entity ownerEntity, float radius, float duration, int damagePerTick, float tickInterval, float stunDuration)
    {
        if (!isServer)
        {
            Debug.LogWarning("[GroundStompArea] SpawnSetup called but isServer=false - skipping.");
            return;
        }

        if (ownerEntity == null)
        {
            Debug.LogError("[GroundStompArea] SpawnSetup failed: ownerEntity is null.");
            return;
        }

        ownerId = ownerEntity.GetNetworkID(true);
        this.radius = radius;
        this.duration = duration;
        this.damagePerTick = damagePerTick;
        this.tickInterval = tickInterval;
        this.stunDuration = stunDuration;

        if (areaCollider != null)
            areaCollider.radius = radius;

        initialized = true;

        StartCoroutine(DamageRoutine());
    }

    private IEnumerator DamageRoutine()
    {
        Debug.Log("[GroundStompArea] DamageRoutine started");

        float timer = duration;

        while (timer > 0f)
        {
            Debug.Log($"[GroundStompArea] Tick - timer={timer:F2}, checking overlaps at {transform.position} radius={radius}");

            Entity ownerEntity = null;
            if (ownerId.HasValue)
                ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);

            if (ownerEntity == null)
            {
                Debug.LogError($"[GroundStompArea] Owner not found for ID={ownerId}. Destroying stomp.");
                Destroy(gameObject);
                yield break;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            Debug.Log($"[GroundStompArea] OverlapSphere hit {hits.Length} colliders");

            foreach (var hit in hits)
            {
                Entity target = Entity.GetEntityFromCollider(hit);
                if (target == null) continue;
                if (target.GetIsDead()) continue;
                if (target == ownerEntity) continue;
                if (target.GetTeam() == ownerEntity.GetTeam()) continue;

                Debug.Log($"[GroundStompArea] Damaging {target.name} for {damagePerTick}");
                target.TakeDamage(damagePerTick, ownerEntity);
                target.ModifyMoveSpeedMultiplier(0f, stunDuration);
            }

            yield return new WaitForSeconds(tickInterval);
            timer -= tickInterval;
        }

        Debug.Log("[GroundStompArea] Duration finished, destroying");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}