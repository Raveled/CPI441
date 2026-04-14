using UnityEngine;
using System.Collections;
using PurrNet;

public class TornadoArea : NetworkBehaviour
{
    [Header("Runtime Setup")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private SphereCollider areaCollider;

    [Header("Owner / Damage")]
    [SerializeField] private NetworkID? ownerId;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private int damagePerTick = 2;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private float groupForce = 10f;
    [SerializeField] private Vector3 travelDirection = Vector3.forward;

    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 4f;
    [SerializeField] private float spiralRadius = 1.5f;
    [SerializeField] private float spiralSpeed = 3f;

    private float timer;
    private float spiralAngle;
    private bool initialized;
    private Vector3 basePosition;

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

    public void SpawnSetup(Entity ownerEntity, float radius, float duration, int damagePerTick, float tickInterval, float groupForce, Vector3 travelDirection)
    {
        if (!isServer)
        {
            Debug.LogWarning("[TornadoArea] SpawnSetup called but isServer=false — skipping.");
            return;
        }

        if (ownerEntity == null)
        {
            Debug.LogError("[TornadoArea] SpawnSetup failed: ownerEntity is null.");
            return;
        }

        ownerId = ownerEntity.GetNetworkID(true);
        this.radius = radius;
        this.duration = duration;
        this.damagePerTick = damagePerTick;
        this.tickInterval = tickInterval;
        this.groupForce = groupForce;
        this.travelDirection = travelDirection.normalized;

        if (areaCollider != null)
            areaCollider.radius = radius;

        timer = duration;
        spiralAngle = 0f;
        basePosition = transform.position;
        initialized = true;

        StartCoroutine(DamageRoutine());
    }

    private void Update()
    {
        if (!isServer || !initialized) return;

        basePosition += travelDirection * forwardSpeed * Time.deltaTime;

        spiralAngle += spiralSpeed * Time.deltaTime;
        Vector3 right = Vector3.Cross(travelDirection, Vector3.up).normalized;
        if (right == Vector3.zero)
            right = Vector3.right;

        Vector3 up = Vector3.Cross(right, travelDirection).normalized;
        if (up == Vector3.zero)
            up = Vector3.up;

        Vector3 spiralOffset =
            (right * Mathf.Cos(spiralAngle) + up * Mathf.Sin(spiralAngle)) * spiralRadius;

        transform.position = basePosition + spiralOffset;
    }

    private IEnumerator DamageRoutine()
    {
        Debug.Log("[TornadoArea] DamageRoutine started");

        while (timer > 0f)
        {
            Debug.Log($"[TornadoArea] Tick — timer={timer:F2}, checking overlaps at {transform.position} radius={radius}");

            Entity ownerEntity = null;
            if (ownerId.HasValue)
                ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);

            if (ownerEntity == null)
            {
                Debug.LogError($"[TornadoArea] Owner not found for ID={ownerId}. Destroying tornado.");
                Destroy(gameObject);
                yield break;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            Debug.Log($"[TornadoArea] OverlapSphere hit {hits.Length} colliders");

            foreach (var hit in hits)
            {
                Entity target = Entity.GetEntityFromCollider(hit);
                if (target == null) continue;
                if (target.GetIsDead()) continue;
                if (target == ownerEntity) continue;
                if (target.GetTeam() == ownerEntity.GetTeam()) continue;

                Debug.Log($"[TornadoArea] Damaging {target.name} for {damagePerTick}");
                target.TakeDamage(damagePerTick, ownerEntity);

                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    Vector3 dirToCenter = (transform.position - target.transform.position).normalized;
                    targetRb.AddForce(dirToCenter * groupForce, ForceMode.Acceleration);
                }
            }

            yield return new WaitForSeconds(tickInterval);
            timer -= tickInterval;
        }

        Debug.Log("[TornadoArea] Duration finished, destroying");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}