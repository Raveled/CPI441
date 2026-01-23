using UnityEngine;

public class NonPlayerEntity : Entity
{
    //Setup fields
    [Header("NPE Setup")]
    [SerializeField] float rewardRange = 50f;
    [SerializeField] protected Transform attackRangeOrigin = null;
    [SerializeField] protected float attackRange = 10f;
    [Space]
    //Debug fields
    [Header("NPE Debug")]
    [SerializeField] bool showRewardRange = false;
    [SerializeField] protected bool showAttackRange = true;
    [Space]
    [SerializeField] protected Transform target;
    [SerializeField] protected float attackCooldownTimer = 0;
    [SerializeField] protected Team enemyTeam = Team.NULL;
    [SerializeField] protected Entity closestPlayer = null;
    [SerializeField] protected Entity closestMinion = null;
    [SerializeField] protected Entity closestTower = null;

    protected override void Start() {
        if (team == Team.TEAM1) {
            enemyTeam = Team.TEAM2;
        } else if (team == Team.TEAM2) {
            enemyTeam = Team.TEAM1;
        }
        base.Start();
    }
    //Overrided Destroy method
    protected override void DestroyThis(Entity damageOrigin) {
        isDead = true;
        DistributeGoldReward();
        base.DestroyThis(damageOrigin);
    }
    //Give the gold reward to the closest players in range
    protected virtual void DistributeGoldReward() {
        /*
         * WIP
         * ***Use this for minions, maybe do a full split among nearest players for towers/creeps
         * 1. get array of all players in range
         * 2. find up to two closest players within range that is not the damageOrigin
         * 3. Give the 1 or 2 selected players the gold reward
         */
    }
    //Find next target
    protected virtual void FindTarget() {
        return;
    }
    //Move
    protected virtual void Move() {
        return;
    }
    //Attack
    protected virtual void Attack() {
        return;
    }
    //Visualization for ranges using Gizmos
    protected virtual void OnDrawGizmos() {
        if (showRewardRange) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rewardRange);
        }
        if (showAttackRange) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackRangeOrigin.position, attackRange);
        }
    }
    //Cooldown for attacks
    protected void AttackTimer() {
        if (attackCooldownTimer >= 0) {
            attackCooldownTimer -= Time.deltaTime;
        }
    }
}
