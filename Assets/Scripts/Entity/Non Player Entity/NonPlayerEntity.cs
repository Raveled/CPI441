using UnityEngine;
using System.Collections.Generic;

public class NonPlayerEntity : Entity
{
    //Setup fields
    [Header("NPE Setup")]
    [SerializeField] float rewardRange = 50f;
    [SerializeField] protected Transform attackRangeOrigin = null;
    [SerializeField] protected float attackRange = 10f;
    [SerializeField] protected NPEDetectLogic npeDetectLogic = null;
    [SerializeField] bool canTargetTower = false;
    [SerializeField] bool canTargetCore = false;
    [SerializeField] bool canTargetMinion = false;
    [SerializeField] bool canTargetPlayer = false;
    [Space]
    //Debug fields
    [Header("NPE Debug")]
    [Tooltip("Yellow Circle")]
    [SerializeField] bool showRewardRange = false;
    [Tooltip("Red Circle")]
    [SerializeField] bool showAttackRange = true;
    [Tooltip("Blue Line")]
    [SerializeField] bool showForwardDirection = true;
    [Space]
    [SerializeField] protected Entity target;
    [SerializeField] protected float attackCooldownTimer = 0;

    //Targeting fields
    protected Entity closestMinion = null;
    protected float minDistanceMinion = Mathf.Infinity;
    protected Entity closestPlayer = null;
    protected float minDistancePlayer = Mathf.Infinity;
    protected Entity closestTower = null;
    protected float minDistanceTower = Mathf.Infinity;
    protected Entity closestCore = null;
    protected float minDistanceCore = Mathf.Infinity;

    protected override void Start() {
        base.Start();
        npeDetectLogic.SetEnemyTeams(enemyTeams);
    }
    //Overrided Destroy method
    protected override void DestroyThis(Entity damageOrigin) {
        isDead = true;

        //Give gold to the closest players
        DistributeGoldReward();

        base.DestroyThis(damageOrigin);
    }
    //Give the gold reward to the closest players in range
    protected virtual void DistributeGoldReward() {
        /*
         * WIP-------------------------------------------------------------------------------------------------------
         * ***Use this for minions and towers
         * 1. get array of all players in range
         * 2. find up to two closest players within range that is not the damageOrigin
         * 3. Give the 1 or 2 selected players the gold reward
         */
    }
    //Move
    protected virtual void Move() {
        if (!canMove) return;
    }
    //Attack
    protected virtual void Attack() {
        if (!canDefaultAttack) return;
    }

    //Getter
    public Entity GetTarget() {
        return target;
    }
    //Cooldown for attacks
    protected void AttackTimer() {
        if (attackCooldownTimer >= 0) {
            attackCooldownTimer -= Time.deltaTime;
        }
    }
    //Gets the closest entity in detect range and sets it as target
    protected virtual void FindTarget() {
        if (!target) {
            //Get List of entities in range
            List<Entity> entitiesInRange = npeDetectLogic.GetEnemiesInRange();
            if (entitiesInRange.Count <= 0) {
                target = null;
                return;
            }

            //Loop through each entity in range
            foreach (Entity e in entitiesInRange) {
                //Get distance from this to entity
                if (!e) continue;
                float dist = Vector3.Distance(transform.position, e.gameObject.transform.position);

                //Set closest entity type
                if (e is Minion && dist < minDistanceMinion) {
                    closestMinion = e;
                    minDistanceMinion = dist;
                } else if (e is Player && dist < minDistancePlayer) {
                    closestPlayer = e;
                    minDistancePlayer = dist;
                }
            }

            //In order of Tower > Core > Minion > Player, set target equal to the closest
            if (canTargetTower && closestTower) target = closestTower;
            else if (canTargetCore && closestCore) target = closestCore;
            else if (canTargetMinion && closestMinion) target = closestMinion;
            else if (canTargetPlayer && closestPlayer) target = closestPlayer;
            else target = null;
        }
    }
    //Resets the target, called from NPEDetectLogic
    public void ResetTarget() {
        closestMinion = null;
        minDistanceMinion = Mathf.Infinity;
        closestPlayer = null;
        minDistancePlayer = Mathf.Infinity;
        closestTower = null;
        minDistanceTower = Mathf.Infinity;
        closestCore = null;
        minDistanceCore = Mathf.Infinity;

        target = null;
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
        if (showForwardDirection) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
        }
    }
}
