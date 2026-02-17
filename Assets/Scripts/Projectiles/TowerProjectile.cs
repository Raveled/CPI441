using UnityEngine;

public class TowerProjectile : Projectile
{
    //Debug
    [Header("Tower Projectile Debug")]
    [Tooltip("Red Circle")]
    [SerializeField] bool showHitRadius = false;
    [Space]
    [Tooltip("This will be equal to the spherecollider radius")]
    [SerializeField] float hitRadius = 1f;

    protected override void LateAwake()
    {
        base.LateAwake();
        hitRadius = GetComponent<SphereCollider>().radius;
    }

    protected override float GetHitRadius() => hitRadius;

    private void OnDrawGizmos()
    {
        if (showHitRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
