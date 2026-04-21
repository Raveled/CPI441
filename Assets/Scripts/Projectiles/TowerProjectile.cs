using UnityEngine;

public class TowerProjectile : Projectile
{
    //Debug
    [Header("Tower Projectile Debug")]
    [Tooltip("Red Circle")]
    [SerializeField] bool showHitRadius = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        hitRadius = GetComponent<SphereCollider>().radius;
    }

    private void OnDrawGizmos()
    {
        if (showHitRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
