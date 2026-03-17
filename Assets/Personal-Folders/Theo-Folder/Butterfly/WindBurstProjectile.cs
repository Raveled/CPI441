using UnityEngine;
using System.Collections;

public class WindBurstProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;

    [HideInInspector] public Entity ownerEntity;
    [HideInInspector] public int damage = 4;
    [HideInInspector] public float maxRange = 6f;
    [HideInInspector] public float radius = 1f;
    [HideInInspector] public float tickInterval = 0.25f;

    private void Start()
    {
        StartCoroutine(DamageRoutine());
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private IEnumerator DamageRoutine()
    {
        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            foreach (var hit in hits)
            {
                Entity target = hit.GetComponent<Entity>();
                if (target == null || ownerEntity == null) continue;
                if (target == ownerEntity || target.GetTeam() == ownerEntity.GetTeam()) continue;

                target.TakeDamage(damage, ownerEntity);
            }

            yield return new WaitForSeconds(tickInterval);
        }
    }
}