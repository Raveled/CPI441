using UnityEngine;
using System.Collections;

public class DazzlingWaveProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    [HideInInspector] public Entity ownerEntity;
    [HideInInspector] public float radius = 4f;
    [HideInInspector] public int damage = 2;
    [HideInInspector] public float tickInterval = 0.5f;
    [HideInInspector] public int healAmount = 10;
    [HideInInspector] public bool upgradeReducedDamage = false;
    [HideInInspector] public float damageReductionMultiplier = 0.7f;

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

                if (target.GetTeam() == ownerEntity.GetTeam())
                {
                    target.Heal(healAmount);
                }
                else
                {
                    target.TakeDamage(damage, ownerEntity);
                    if (upgradeReducedDamage)
                        target.ModifyAttackPowerForSeconds(damageReductionMultiplier, 3f);
                }
            }

            yield return new WaitForSeconds(tickInterval);
        }
    }
}