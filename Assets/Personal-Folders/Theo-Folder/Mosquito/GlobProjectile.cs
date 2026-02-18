using UnityEngine;
using PurrNet;

public class GlobProjectile : NetworkBehaviour
{
    public SyncVar<int> syncDamage = new(0);
    public SyncVar<float> syncSpeed = new(0f);
    public SyncVar<NetworkID?> syncOwnerID = new(null);

    private bool despawned = false;
    private Entity cachedOwner = null;

    protected override void OnSpawned()
    {
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
    }

    private void Update()
    {
        if (despawned) return;
        transform.Translate(Vector3.forward * syncSpeed.value * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (despawned || !isServer) return;

        if (cachedOwner != null)
        {
            if (other.transform.IsChildOf(cachedOwner.transform) || other.gameObject == cachedOwner.gameObject)
                return;
        }

        Entity target = other.GetComponent<Entity>();
        if (target == null) return;
        if (cachedOwner != null && (target == cachedOwner || target.GetTeam() == cachedOwner.GetTeam())) return;

        target.TakeDamage(syncDamage.value, cachedOwner);
        despawned = true;
        Despawn();
    }
}