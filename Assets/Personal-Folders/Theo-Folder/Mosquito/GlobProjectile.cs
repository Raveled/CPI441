using UnityEngine;

public class GlobProjectile : MonoBehaviour
{
    [HideInInspector] public Entity ownerEntity;  // Changed from Mosquito
    [HideInInspector] public int damage;
    [HideInInspector] public float speed;

    private void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Entity target = other.GetComponent<Entity>();
        if (target != null && target != ownerEntity)
        {
            target.TakeDamage(damage, ownerEntity);
        }
        Destroy(gameObject);
    }
}