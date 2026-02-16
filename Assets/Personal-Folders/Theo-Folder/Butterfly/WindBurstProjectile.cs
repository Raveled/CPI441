using UnityEngine;

public class WindBurstProjectile : MonoBehaviour
{
    [HideInInspector] public Entity ownerEntity;
    [HideInInspector] public int damage;
    [HideInInspector] public float speed;
    [HideInInspector] public float maxRange = 5f;

    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPos, transform.position) >= maxRange)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Entity target = other.GetComponent<Entity>();
        if (target != null && target != ownerEntity && target.GetTeam() != ownerEntity.GetTeam())
        {
            target.TakeDamage(damage, ownerEntity);
            Destroy(gameObject);
        }
    }
}