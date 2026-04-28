using UnityEngine;
using PurrNet;
using System.Collections;

public class DazzlingWaveProjectile : Projectile
{
    [Header("Dazzling Wave Settings")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private float tickInterval = 0.5f;
    [SerializeField] private int healAmount = 10;
    [SerializeField] private float movementSpeed = 5f;

    private float currentDuration = 0f;
    private float tickTimer = 0f;
    private bool isProcessing = false;
    private Vector3 moveDirection;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (isServer)
        {
            currentDuration = 0f;
            tickTimer = 0f;
            isProcessing = true;

            // Disable the base projectile's collider if needed, or keep it for detection
            if (hitCollider != null)
            {
                hitCollider.isTrigger = true;
            }

            // Get the movement direction (forward from spawn rotation)
            moveDirection = transform.forward;

            // Ensure the projectile doesn't spin by freezing rotation
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    protected void Update()
    {
        if (!isServer || !isProcessing) return;

        // Move the projectile forward
        if (movementSpeed > 0f)
        {
            transform.position += moveDirection * movementSpeed * Time.deltaTime;
        }

        // Track duration
        currentDuration += Time.deltaTime;
        if (currentDuration >= duration)
        {
            DestroyProjectile();
            return;
        }

        // Process ticks
        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            ApplyTickEffect();
        }
    }

    private void ApplyTickEffect()
    {
        if (!isServer || !isProcessing) return;

        Entity ownerEntity = Entity.GetEntityByNetworkID(ownerId.Value, isServer);
        if (ownerEntity == null)
        {
            Debug.LogError($"[DazzlingWave] ApplyTickEffect - owner not found for ID={ownerId}");
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hitRadius);
        int enemiesHit = 0;
        int friendliesHit = 0;

        foreach (Collider c in hitColliders)
        {
            Entity e = Entity.GetEntityFromCollider(c);
            if (e == null) continue;
            if (e.GetIsDead()) continue;

            // Check if target is friendly or enemy
            if (enemyTeams.Contains(e.GetTeam()))
            {
                // Enemy - take damage
                Debug.Log($"[DazzlingWave] Tick - Hit enemy {e.name} for {damage} damage!");
                e.TakeDamage(damage, ownerEntity);
                enemiesHit++;
            }
            else if (e.GetTeam() == ownerEntity.GetTeam())
            {
                // Friendly - heal (only same team)
                Debug.Log($"[DazzlingWave] Tick - Healed friendly {e.name} for {healAmount} health!");
                e.Heal(healAmount);
                friendliesHit++;
            }
        }

        if (enemiesHit > 0 || friendliesHit > 0)
        {
            Debug.Log($"[DazzlingWave] Tick complete - Hit {enemiesHit} enemies, healed {friendliesHit} allies");
        }
    }

    private void DestroyProjectile()
    {
        if (!isServer || !isProcessing) return;

        isProcessing = false;
        Debug.Log($"[DazzlingWave] Duration ended ({duration}s) - destroying projectile");

        // Destroy the GameObject
        Destroy(gameObject);
    }

    // Override base OnTriggerEnter to prevent immediate detonation
    protected override void OnTriggerEnter(Collider other)
    {
        // Do nothing - we want the projectile to persist and tick over time
        // The base implementation would call Detonate() immediately
        return;
    }

    // Override base Detonate to prevent immediate destruction
    protected new void Detonate()
    {
        // Do nothing - we want the projectile to persist
        // Regular destruction happens through the duration timer
    }

    // Override base ApplyDamage to prevent immediate damage application
    protected override void ApplyDamage()
    {
        // Do nothing - damage is applied over time in ApplyTickEffect
    }
}