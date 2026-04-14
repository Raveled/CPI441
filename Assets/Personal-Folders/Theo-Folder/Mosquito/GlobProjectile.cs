using UnityEngine;
using PurrNet;

public class GlobProjectile : Projectile
{
    private Entity struckTarget = null;

    protected override void OnTriggerEnter(Collider other)
    {
        if (!isServer || !isActive) return;

        Entity ownerEntity = null;
        if (ownerId.HasValue)
        {
            ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
        }

        if (ownerEntity != null)
        {
            if (other.transform.IsChildOf(ownerEntity.transform) || other.gameObject == ownerEntity.gameObject)
            {
                return;
            }
        }

        Entity target = Entity.GetEntityFromCollider(other);
        if (target == null)
        {
            return;
        }

        if (target.GetIsDead())
        {
            return;
        }

        if (ownerEntity != null)
        {
            if (target == ownerEntity) return;
            if (target.GetTeam() == ownerEntity.GetTeam()) return;
        }

        struckTarget = target;

        Debug.Log($"[Glob] Valid hit on {target.name} - detonating");
        Detonate();
    }

    protected override void ApplyDamage()
    {
        if (!isServer || !isActive) return;

        Entity ownerEntity = null;
        if (ownerId.HasValue)
        {
            ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
        }

        if (ownerEntity == null)
        {
            Debug.LogError($"[Glob] ApplyDamage - owner not found for ID={ownerId}");
            return;
        }

        if (struckTarget == null)
        {
            Debug.LogWarning("[Glob] ApplyDamage called, but no struckTarget was recorded.");
            return;
        }

        if (struckTarget.GetIsDead())
        {
            return;
        }

        if (struckTarget == ownerEntity)
        {
            return;
        }

        if (struckTarget.GetTeam() == ownerEntity.GetTeam())
        {
            return;
        }

        Debug.Log($"[Glob] Hit {struckTarget.name} for {damage} damage!");
        struckTarget.TakeDamage(damage, ownerEntity);
    }
}