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

    protected override void Start()
    {
        base.Start();
        hitRadius = GetComponent<SphereCollider>().radius;
    }
    private void OnTriggerEnter(Collider other)
    {
        //If collision is not a tower or NPEDetectLogic, detonate
        if(!other.gameObject.GetComponent<Tower>() && !other.gameObject.GetComponent<NPEDetectLogic>())
        {
            Detonate();
        }
    }
    void Detonate()
    {
        //Get all hit colliders
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hitRadius);

        bool onlyHitTarget = target ? true : false;

        foreach(Collider c in hitColliders)
        {
            if(c.gameObject.TryGetComponent<Entity>(out Entity e))
            {
                //If not AOE attack, only hit target
                if (onlyHitTarget && e != target) continue;

                //If collider is entity on enemy team, deal damage to it
                if (enemyTeams.Contains(e.GetTeam()))
                {
                    e.TakeDamage(damage, owner);
                }
            }
        }

        //Destroy object upon damage dealt
        Destroy(gameObject);
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
