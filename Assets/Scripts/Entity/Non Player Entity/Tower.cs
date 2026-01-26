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
        //FindTarget();
        Attack();
        AttackTimer();
    }
    //protected override void FindTarget() {
    //    //WIP-------------------------------------------------------------------------------------------------------

    //    //If no target, find target
    //    if (!target) {
    //        //Find all entities
    //        Entity[] found = FindObjectsByType<Entity>(FindObjectsSortMode.None);

    //        //vars for closest minion and player, and their minimum distances
    //        Transform closestMinion = null;
    //        float minDistanceMinion = Mathf.Infinity;
    //        Transform closestPlayer = null;
    //        float minDistancePlayer = Mathf.Infinity;

    //        //Loop through all found entities
    //        for (int i = 0; i < found.Length; i++) {
    //            Entity e = found[i];

    //            //If the entity is not an enemy, ignore
    //            if (e.GetTeam() != enemyTeam) continue;

    //            //Check distance
    //            float distance = Vector3.Distance(attackRangeOrigin.position, e.transform.position);
    //            if (distance < attackRange) {
    //                if (e is Minion && distance < minDistanceMinion) {
    //                    minDistanceMinion = distance;
    //                    closestMinion = e;
    //                } else if (e is Entity && distance < minDistancePlayer) //THIS SHOULD BE PLAYER
    //                {
    //                    minDistancePlayer = distance;
    //                    closestPlayer = e;
    //                }
    //            }
    //        }

    //        //In order of Minion > Player > None, set target
    //        if (closestMinion) return closestMinion;
    //        else if (closestPlayer) return closestPlayer;
    //        else return null;
    //    }
    //    //If target, check if the target is within range or not
    //    else {
    //        if (Vector3.Distance(attackRangeOrigin.position, target.position) > attackRange) {
    //            target = null;
    //        }
    //    }
    //}
    protected override void Attack() {
        //If there is a current target and the attack cooldown is ready
        if (target && attackCooldownTimer <= 0) {
            //Reset attack cooldown
            attackCooldownTimer = defaultAttackCooldown;

            //Get direction between target and this tower
            Vector3 direction = (target.position - attackRangeOrigin.position).normalized;

            //Instantiate new projectile and set it's properties
            TowerProjectile proj = Instantiate(towerProjectile, attackRangeOrigin.position, transform.rotation, this.transform);
            Entity e = new Entity();
            proj.SpawnSetup(e, 0, Vector3.up, 1  );
        }
    }
    public override void TakeDamage(int damage, Entity damageOrigin) {
        //If the protector tower is alive, this takes no damage
        if (!protector || protector.GetIsDead()) {
            base.TakeDamage(damage, damageOrigin);
        }
    }
    //When a player attacks another player within the range of the tower
    public void OverrideTarget(Entity damageOrigin) {
        //WIP-------------------------------------------------------------------------------------------------------
        //Check if Enemy player that dealt the damage is within range of this tower
        float distance = Vector3.Distance(attackRangeOrigin.position, damageOrigin.gameObject.transform.position);
        if(distance < attackRange)
        {
            //Set the target of this tower to the enemy player that dealt damage
            target = damageOrigin.transform;
        }
    }
}
