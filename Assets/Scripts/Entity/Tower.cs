using UnityEngine;
using UnityEngine.TextCore.Text;

public class Tower : NonPlayerEntity
{
    [Header("Tower Setup")]
    [SerializeField] TowerProjectile towerProjectile = null;
    [SerializeField] float projectileSpeed = 5f;
    [Tooltip("Only Core & Guardian Tower need this")]
    [SerializeField] Entity protector;
    protected override void Start() {
        base.Start();
    }

    void Update() {
        CheckTargetRange();
        FindTarget();
        Attack();
        AttackTimer();
    }
    protected override void FindTarget() {
        if (!target) {
            Entity t = FindClosestEntity<Entity>();
            if (t) target = t.transform;
        }
    }
    //Takes general Entity type and returns either closest Minion or Player, prioritizing Minion
    public T FindClosestEntity<T>() where T : Entity
    {
        //WIP-------------------------------------------------------------------------------------------------------
        T[] found = FindObjectsByType<T>(FindObjectsSortMode.None);

        T closestMinion = null;
        float minDistanceMinion = Mathf.Infinity;
        T closestPlayer = null;
        float minDistancePlayer = Mathf.Infinity;

        for (int i = 0; i < found.Length; i++)
        {
            var e = found[i];
            if (e.GetTeam() != enemyTeam) continue;
            float distance = Vector3.Distance(attackRangeOrigin.position, e.transform.position);
            if(e is Minion)
            {
                if (distance < minDistanceMinion && distance < attackRange)
                {
                    minDistanceMinion = distance;
                    closestMinion = e;
                }
            } else if(e is Entity) //THIS SHOULD BE PLAYER
            {
                if (distance < minDistancePlayer && distance < attackRange)
                {
                    minDistancePlayer = distance;
                    closestPlayer = e;
                }
            } 
        }
        if (closestMinion) return closestMinion;
        else if (closestPlayer) return closestPlayer;
        else return null;
    }
    protected override void Attack() {
        if (target && attackCooldownTimer <= 0) {
            attackCooldownTimer = defaultAttackCooldown;
            Debug.Log("Is attacking: " + target.gameObject.name);
            Vector3 direction = (target.position - attackRangeOrigin.position).normalized;
            Debug.Log("dir vec: " + direction);
            TowerProjectile proj = Instantiate(towerProjectile, attackRangeOrigin.position, transform.rotation, this.transform);
            Entity e = new Entity();
            proj.SpawnSetup(e, 0, Vector3.up, 1  );
        }
    }
    public override void TakeDamage(int damage, Entity damageOrigin) {
        if (!protector || protector.GetIsDead()) {
            base.TakeDamage(damage, damageOrigin);
        }
    }
    //When a player attacks another player within the range of the tower
    public void OverrideTarget(Entity harmedPlayer, Entity damageOrigin) {
        //WIP-------------------------------------------------------------------------------------------------------
        //*when player takes damage, find closest tower on same team and call this method on it
        float distance = Vector3.Distance(harmedPlayer.transform.position, damageOrigin.transform.position);
        if(distance < attackRange)
        {
            target = damageOrigin.transform;
        }
    }
    //Continuously check if the target is within range or not
    void CheckTargetRange() {
        if (target) {
            if (Vector3.Distance(attackRangeOrigin.position, target.position) > attackRange) {
                target = null;
            }
        }
    }
    
}
