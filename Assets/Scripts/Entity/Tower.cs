using UnityEngine;

public class Tower : NonPlayerEntity
{
    [Header("Tower Setup")]
    [Tooltip("Only Core & Guardian Tower need this")][SerializeField] Entity protector;
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
        //WIP
        if (!target) {
            /*
             * 1. target closest minion within range
             * 2. target closest player within range
             * 3. otherwise null
             */
        }
    }
    protected override void Attack() {
        //WIP
        if (target && attackCooldownTimer <= 0) {
            //attack
            attackCooldownTimer = defaultAttackCooldown;
        }
    }
    protected override void TakeDamage(int damage, Entity damageOrigin) {
        if (!protector || protector.GetIsDead()) {
            base.TakeDamage(damage, damageOrigin);
        }
    }
    //When a player attacks another player within the range of the tower
    public void OverrideTarget(Entity harmedPlayer, Entity damageOrigin) {
        //WIP
        /*
         * ***when a player on same team takes damage, its damage script should call this
         * 1. if damageOrigin is in tower range, the target immediately becomes them
         */
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
