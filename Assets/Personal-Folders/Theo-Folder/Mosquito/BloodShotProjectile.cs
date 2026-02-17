using UnityEngine;
using PurrNet;

public class BloodShotProjectile : NetworkBehaviour
{
    public SyncVar<int> syncDamage = new(0);
    public SyncVar<float> syncSpeed = new(0f);
    public SyncVar<float> syncMaxRange = new(8f);
    public SyncVar<NetworkID?> syncOwnerID = new(null);

    private Vector3 startPos;
    private bool despawned = false;
    private Entity cachedOwner = null;

    private void Awake()
    {
        startPos = transform.position;
    }

    protected override void OnSpawned()
    {
        // Resolve owner from synced NetworkID — works on all clients
        if (syncOwnerID.value.HasValue)
        {
            Entity[] all = FindObjectsByType<Entity>(FindObjectsSortMode.None);
            foreach (var e in all)
            {
                if (e.GetNetworkID(isServer) == syncOwnerID.value)
                {
                    cachedOwner = e;
                    break;
                }
            }
        }
        Debug.Log($"[BloodShot] OnSpawned — isServer={isServer}, owner={cachedOwner?.name}, syncSpeed={syncSpeed.value}, syncMaxRange={syncMaxRange.value}");
    }

    private void Update()
    {
        if (despawned) return;

        transform.Translate(Vector3.forward * syncSpeed.value * Time.deltaTime);

        if (isServer && syncMaxRange.value > 0 && Vector3.Distance(startPos, transform.position) >= syncMaxRange.value)
        {
            despawned = true;
            Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (despawned || !isServer) return;

        // Use cachedOwner if available, otherwise skip owner checks
        if (cachedOwner != null)
        {
            if (other.transform.IsChildOf(cachedOwner.transform) || other.gameObject == cachedOwner.gameObject)
                return;
        }

        Entity target = other.GetComponent<Entity>();
        if (target == null) return;
        if (cachedOwner != null && (target == cachedOwner || target.GetTeam() == cachedOwner.GetTeam())) return;

        Debug.Log($"[BloodShot] Hit {target.name} for {syncDamage.value} damage!");
        target.TakeDamage(syncDamage.value, cachedOwner);

        if (cachedOwner != null)
        {
            Mosquito mosquito = cachedOwner.GetComponent<Mosquito>();
            if (mosquito != null)
                mosquito.OnBasicAttackHit(target);
        }

        despawned = true;
        Despawn();
    }
}