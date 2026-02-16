using UnityEngine;
using System.Collections;

public class TornadoArea : MonoBehaviour
{
    [HideInInspector] public Entity ownerEntity;
    [HideInInspector] public float radius = 5f;
    [HideInInspector] public float duration = 4f;
    [HideInInspector] public int damagePerTick = 2;
    [HideInInspector] public float tickInterval = 0.5f;
    [HideInInspector] public float groupForce = 10f;

    private float timer;

    private void OnEnable()
    {
        timer = duration;
        StartCoroutine(TornadoRoutine());
    }

    private IEnumerator TornadoRoutine()
    {
        while (timer > 0f)
        {
            timer -= tickInterval;

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                Entity target = hit.GetComponent<Entity>();
                if (target == null || target == ownerEntity || target.GetTeam() == ownerEntity.GetTeam())
                    continue;

                // Low damage per tick
                target.TakeDamage(damagePerTick, ownerEntity);

                // Group enemies toward center
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dirToCenter = (transform.position - target.transform.position).normalized;
                    rb.AddForce(dirToCenter * groupForce, ForceMode.Acceleration);
                }
            }

            yield return new WaitForSeconds(tickInterval);
        }

        Destroy(gameObject);
    }
}