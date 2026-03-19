using UnityEngine;
using PurrNet;

public class BloodShotProjectile : Projectile
{
    protected override void LateAwake()
    {
        base.LateAwake();

        // Apply velocity after base LateAwake initializes the prediction system
        if (rb != null && currentState.isActive)
        {
            rb.linearVelocity = currentState.velocity;
            Debug.Log($"[BloodShot] LateAwake - velocity={currentState.velocity}, isServer={isServer}");
        }
        else
        {
            //Debug.LogWarning($"[BloodShot] LateAwake - rb={rb}, isActive={currentState.isActive}");
        }
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        // Ignore towers and NPE detection logic (inherited behaviour from Projectile)
        if (other.gameObject.GetComponent<Tower>() || other.gameObject.GetComponent<NPEDetectLogic>())
            return;

        // Ignore the owner
        Entity ownerEntity = GetEntityByNetworkID(currentState.ownerId.Value);
        if (ownerEntity != null)
        {
            if (other.transform.IsChildOf(ownerEntity.transform) || other.gameObject == ownerEntity.gameObject)
                return;
        }

        // Ignore non-entities
        Entity target = other.GetComponent<Entity>();
        if (target == null) return;

        // Ignore friendly targets
        if (ownerEntity != null && target.GetTeam() == ownerEntity.GetTeam()) return;

        Detonate();
    }

    protected override void ApplyDamage()
    {
        if (!isServer) return;

        var state = currentState;
        Entity ownerEntity = GetEntityByNetworkID(state.ownerId.Value);
        if (ownerEntity == null) return;

        Entity target = GetEntityByNetworkID(state.targetId.HasValue ? state.targetId.Value : default);

        // BloodShot hits whatever it collided with directly via overlap at position
        Collider[] hitColliders = Physics.OverlapSphere(state.position, GetHitRadius());
        foreach (Collider c in hitColliders)
        {
            Entity e = c.GetComponent<Entity>();
            if (e == null || e.GetIsDead()) continue;
            if (e == ownerEntity || e.GetTeam() == ownerEntity.GetTeam()) continue;
            if (!state.enemyTeams.Contains(e.GetTeam())) continue;

            Debug.Log($"[BloodShot] Hit {e.name} for {state.damage} damage!");
            e.TakeDamage(state.damage, ownerEntity);

            // Notify Mosquito so it can gain blood meter
            Mosquito mosquito = ownerEntity.GetComponent<Mosquito>();
            if (mosquito != null)
                mosquito.OnBasicAttackHit(e);
        }
    }

    protected override float GetHitRadius() => 0.5f;
}