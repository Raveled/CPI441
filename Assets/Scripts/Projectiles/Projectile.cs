using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

using PurrNet;
using PurrNet.Prediction;
using PurrNet.Modules;


//Superclass for all projectiles

public struct ProjectileState : IPredictedData<ProjectileState>
{
    public Vector3 position;
    public Vector3 velocity;
    public NetworkID? ownerId; // The ID of spawner
    public NetworkID? targetId; // Optional target (null if AOE)
    public int damage;
    public bool isActive;
    public List<Entity.Team> enemyTeams;

    public void Dispose() { }
}

public class Projectile : PredictedIdentity<ProjectileState>
{
    [Header("Projectile Setup")]
    [Tooltip("Must be set in inspector")]
    [SerializeField] protected PredictedRigidbody rb;
    [SerializeField] protected SphereCollider hitCollider;

    //Info when spawned
    [Header("Projectile Debug")]
    [SerializeField] protected NetworkID? ownerId;
    [SerializeField] protected int damage;
    [SerializeField] protected List<Entity.Team> enemyTeams;
    [SerializeField] protected NetworkID? target;

    private PredictedEvent _onDetonate;

    protected override void LateAwake()
    {
        base.LateAwake();
        _onDetonate = new PredictedEvent(predictionManager, this);
        
        // Cache components
        if (!rb) 
        {
            Debug.Log("Tower -- Missing Predicted Rigidbody reference, attempting to cache.");
            rb = GetComponent<PredictedRigidbody>();
        }
        if (!hitCollider) 
        {
            Debug.Log("Tower -- Missing Hit Collider reference, attempting to cache.");
            hitCollider = GetComponent<SphereCollider>();
        }

        if (isServer)
        {
            // Server: enable physics and collision
            rb.isKinematic = false;
            GetComponent<Collider>().enabled = true;
            GetComponent<Collider>().isTrigger = true;
        }
        else
        {
            // Clients: disable physics, only interpolate
            rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }
    }

    //Called by the script that spawns this object
    public virtual void SpawnSetup(Entity ownerEntity, int damage, Vector3 direction, float speed, Entity targetEntity = null)
    {
        //Get NetworkID from Target
        NetworkID? targetNetId = null;
        if (targetEntity != null)
        {
            targetNetId = targetEntity.GetNetworkID(isServer);
        }

        //Setup
        var newState = currentState;
        newState.position = transform.position;
        newState.velocity = direction.normalized * speed;
        newState.ownerId = ownerEntity.GetNetworkID(isServer);
        newState.targetId = targetNetId;
        newState.damage = damage;
        newState.isActive = true;
        newState.enemyTeams = ownerEntity.GetEnemyTeams();

        currentState = newState;

        if (rb)
        {
            rb.linearVelocity = newState.velocity;
        }

        // Debug Caching
        this.ownerId = currentState.ownerId;
        this.damage = damage;
        this.enemyTeams = newState.enemyTeams;
        this.target = targetNetId;
    }

    protected override void Simulate(ref ProjectileState state, float delta)
    {
        if (!state.isActive)
            return;

        // Apply velocity to rigidbody instead of manual position update
        if (rb)
        {
            state.velocity = rb.linearVelocity;
        }
        // Update state position from actual position
        state.position = transform.position;
    }

    protected override void GetUnityState(ref ProjectileState state)
    {
        state.position = transform.position;
        if (rb)
        {
            state.velocity = rb.linearVelocity;
        }
    }

    protected override void SetUnityState(ProjectileState state)
    {
        transform.position = state.position;
        if (rb)
        {
            rb.linearVelocity = state.velocity; 
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!isServer)
            return;

        //If collision is not a tower or NPEDetectLogic, detonate
        if(!other.gameObject.GetComponent<Tower>() && !other.gameObject.GetComponent<NPEDetectLogic>())
        {
            Detonate();
        }
    }

    protected virtual void Detonate()
    {
        if (!isServer)
            return;

        var state = currentState;
        state.isActive = false;
        currentState = state;

        // Broadcast detonation event to all clients
        _onDetonate.Invoke();

        // Apply damage server-side
        ApplyDamage();

        Destroy(gameObject);
    }

    protected virtual void ApplyDamage()
    {
        if (!isServer)
            return;

        var state = currentState;
        Entity ownerEntity = GetEntityByNetworkID(state.ownerId.Value);
        Entity targetEntity = null;
        
        if (state.targetId.HasValue)
        {
            targetEntity = GetEntityByNetworkID(state.targetId.Value);
        }

        if (!ownerEntity)
            return;

        var enemyTeams = ownerEntity.GetEnemyTeams();

        // AOE or single-target damage
        Collider[] hitColliders = Physics.OverlapSphere(state.position, GetHitRadius());

        bool onlyHitTarget = state.targetId.HasValue; // If there's a target, only hit that target

        foreach (Collider c in hitColliders)
        {
            if (c.TryGetComponent<Entity>(out Entity e))
            {
                if (e.GetIsDead()) continue;

                //If not AOE attack, only hit target
                if (onlyHitTarget && e != targetEntity) continue;

                //If collider is entity on enemy team, deal damage to it
                if (enemyTeams.Contains(e.GetTeam()))
                {
                    e.TakeDamage(state.damage, ownerEntity);
                }
            }
        }
    }

    // Helper method to find Entity by NetworkID (same pattern as Entity class)
    protected Entity GetEntityByNetworkID(NetworkID networkId)
    {
        Entity[] allEntities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
        foreach (var entity in allEntities) {
            if (entity.GetNetworkID(isServer) == networkId) {

                return entity;
            }
        }

        return null;
    }

    protected virtual float GetHitRadius() => 1f;
}
