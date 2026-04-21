using UnityEngine;
using PurrNet;

public class DazzlingWaveProjectile : Projectile
{
    protected override void OnTriggerEnter(Collider other)
    {
        if (!isServer || !isActive) return;
        Entity target = other.GetComponent<Entity>();

        // Ignore non-entities
        if (target == null)
        {
            return;
        }

        // Ignore owner
        if (ownerId.HasValue)
        {
            Entity ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
            if (ownerEntity != null)
            {
                if (other.transform.IsChildOf(ownerEntity.transform) || ownerId == target.GetNetworkID(isServer))
                {
                    return;
                }
            }
        }

        // Ignore friendlies
        if (ownerId.HasValue)
        {
            Entity ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
            if (ownerEntity != null && target.GetTeam() == ownerEntity.GetTeam())
            {
                return;
            }
        }

        Debug.Log($"[DazzlingWave] Valid hit on {target.name} - detonating");
        Detonate();
    }

    protected override void ApplyDamage()
    {
        if (!isServer || !isActive) return;

        Entity ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
        if (ownerEntity == null)
        {
            Debug.LogError($"[DazzlingWave] ApplyDamage - owner not found for ID={ownerId}");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hitRadius);

        foreach (Collider c in hitColliders)
        {
            Entity e = Entity.GetEntityFromCollider(c);
            if (e == null) continue;
            if (e.GetIsDead()) continue;
            if (e == ownerEntity) continue;
            if (e.GetTeam() == ownerEntity.GetTeam()) continue;

            Debug.Log($"[DazzlingWave] Hit {e.name} for {damage} damage!");
            e.TakeDamage(damage, ownerEntity);
        }
    }
}