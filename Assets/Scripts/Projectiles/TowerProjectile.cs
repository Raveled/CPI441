using UnityEngine;

public class TowerProjectile : Projectile
{
    //Setup
    [Header("Tower Projectile Setup")]
    [SerializeField] float hitRadius = 1f;
    [Space]
    //Debug
    [Header("Tower Projectile Debug")]
    [Tooltip("Red Circle")]
    [SerializeField] bool showHitRadius = false;

    protected override void Start()
    {
        base.Start();
    }
    private void OnTriggerEnter(Collider other)
    {
        if(!other.gameObject.TryGetComponent<Tower>(out Tower t))
        {
            Debug.Log("detonating from: " + other.gameObject.name);
            Detonate();
        }
    }
    void Detonate()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, hitRadius);
        foreach(Collider c in hitColliders)
        {
            if(c.gameObject.TryGetComponent<Entity>(out Entity e))
            {
                if (enemyTeams.Contains(e.GetTeam()))
                {
                    Debug.Log(owner.GetName() + "'s projectile is dealing damage to: " + e.GetName());
                    e.TakeDamage(damage, owner);
                }
            }
        }
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
