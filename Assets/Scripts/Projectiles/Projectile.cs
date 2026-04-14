using UnityEngine;
using System.Collections.Generic;
using PurrNet;

public class Projectile : NetworkBehaviour
{
    [Header("Projectile Setup")]
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected SphereCollider hitCollider;

    [Header("Projectile Settings")]
    [SerializeField] protected float maxLifetime = 3f;
    [SerializeField] protected float hitRadius = 1f;

    [Header("Projectile Debug")]
    [SerializeField] protected NetworkID? ownerId;
    [SerializeField] protected int damage;
    [SerializeField] protected List<Entity.Team> enemyTeams;
    [SerializeField] protected NetworkID? targetId;

    private float lifetime;
    protected bool isActive;

    protected void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!hitCollider) hitCollider = GetComponent<SphereCollider>();
    }

    // This replaces the old LateAwake physics setup that was lost in the refactor
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer)
        {
            rb.isKinematic = false;
            hitCollider.enabled = true;
            hitCollider.isTrigger = true;
        }
        else
        {
            if (!isServer)
            {
                rb.isKinematic = true;
                hitCollider.enabled = false;
            }
        }
    }

    public void SpawnSetup(Entity ownerEntity, int damage, Vector3 direction, float speed, Entity targetEntity = null)
    {
        if (!isServer)
        {
            Debug.LogWarning("[Projectile] SpawnSetup called but isServer=false — skipping.");
            return;
        }

        ownerId = ownerEntity.GetNetworkID(true);
        targetId = targetEntity ? targetEntity.GetNetworkID(true) : null;
        this.damage = damage;
        this.enemyTeams = ownerEntity.GetEnemyTeams();
        lifetime = maxLifetime;
        isActive = true;

        rb.linearVelocity = direction.normalized * speed;
    }

    protected void Update()
    {
        if (!isServer || !isActive) return;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
        {
            Detonate();
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isServer || !isActive) return;

        Debug.Log($"[Projectile] OnTriggerEnter with {other.gameObject.name}");

        if (!Entity.GetEntityFromCollider(other))
            Detonate();
    }

    protected void Detonate()
    {
        if (!isServer || !isActive) return;

        ApplyDamage();
        isActive = false;
        Destroy(gameObject);
    }

    protected virtual void ApplyDamage()
    {
        if (!isServer || !isActive) return;

        Entity ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
        if (!ownerEntity)
        {
            Debug.LogError($"[Projectile] ApplyDamage - could not find owner entity for ID={ownerId}");
            return;
        }

        Entity targetEntity = targetId.HasValue ? Entity.GetEntityByNetworkID(targetId.Value, isServer) : null;
        bool onlyHitTarget = targetId.HasValue;

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);

        foreach (Collider c in hits)
        {
            Entity e = Entity.GetEntityFromCollider(c);
            if (!e) continue;
            if (e.GetIsDead()) { continue; }
            if (onlyHitTarget && e != targetEntity) { continue; }

            if (enemyTeams.Contains(e.GetTeam()))
            {
                Debug.Log($"[Projectile] Dealing {damage} damage to {e.name}");
                e.TakeDamage(damage, ownerEntity);
            }
        }
    }
}