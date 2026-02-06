using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions.Must;

public class NonPlayerEntity : Entity
{
    //Setup fields
    [Header("NPE Setup")]
    [SerializeField] protected float attackRange = 10f;
    [Space]
    [SerializeField] protected Transform attackRangeOrigin = null;
    [SerializeField] protected NPEDetectLogic npeDetectLogic = null;
    [SerializeField] protected UnityEngine.UI.Slider healthBar = null;
    [Space]
    [SerializeField] bool canTargetTower = false;
    [SerializeField] bool canTargetCore = false;
    [SerializeField] bool canTargetMinion = false;
    [SerializeField] bool canTargetPlayer = false;
    [Space]
    //Debug fields
    [Header("NPE Debug")]
    [Tooltip("Red Circle")]
    [SerializeField] bool showAttackRange = true;
    [Tooltip("Blue Line")]
    [SerializeField] bool showForwardDirection = true;
    [Space]
    [SerializeField] protected Entity target;
    [SerializeField] protected bool hasTarget = false;
    [SerializeField] protected float attackCooldownTimer = 0;
    [SerializeField] bool canSearchForTarget = true;
    [SerializeField] bool canAttackTimer = true;
    [SerializeField] List<Entity> entitiesInRange; //for debug purposes, used in FindTarget()


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
        entitiesInRange = new List<Entity>();
    }
    public override bool TakeDamage(int damage, Entity damageOrigin)
    {
        bool temp = base.TakeDamage(damage, damageOrigin);
        UpdateHealthBar();
        return temp;
    }
    public override void Heal(int healAmount)
    {
        base.Heal(healAmount);
        UpdateHealthBar();
    }
    public override void GainMaxHealth(int maxHealthAmount)
    {
        base.GainMaxHealth(maxHealthAmount);
        UpdateHealthBar();
    }
    //Update healthBar UI Element
    void UpdateHealthBar()
    {
        healthBar.maxValue = maximumHitPoints;
        healthBar.value = currentHitPoints;
    }
    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);

        //Destroy self
        Destroy(gameObject);
    }
    //Move
    protected virtual void Move() {
        if (!canMove) return;
    }
    //Attack
    protected virtual void Attack() {
        if (!canDefaultAttack) return;
    }
    //Cooldown for attacks
    protected void AttackTimer() {
        if (!canAttackTimer) return;

        if (attackCooldownTimer >= 0) {
            attackCooldownTimer -= Time.deltaTime;
        }
    }
    //Gets the closest entity in detect range and sets it as target
    protected virtual void FindTarget() {
        if (!canSearchForTarget) return;

        if (!target) {
            //Get List of entities in range
            entitiesInRange = npeDetectLogic.GetEnemiesInRange();
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
                } else if (e is Tower && dist < minDistanceTower) {
                    closestTower = e;
                    minDistanceTower = dist;
                } else if (e is Core && dist < minDistanceCore) {
                    closestCore = e;
                    minDistanceCore = dist;
                }
            }

            //In order of Tower > Core > Minion > Player, set target equal to the closest
            if (canTargetTower && closestTower) target = closestTower;
            else if (canTargetCore && closestCore) target = closestCore;
            else if (canTargetMinion && closestMinion) target = closestMinion;
            else if (canTargetPlayer && closestPlayer) target = closestPlayer;
            else target = null;

            if (target != null) hasTarget = true;
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
    protected override void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (showAttackRange) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackRangeOrigin.position, attackRange);
        }
        if (showForwardDirection) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
        }
    }
    //For pausing the game
    public void Freeze(bool freezeNPE) {
        canMove = !freezeNPE;
        canDefaultAttack = !freezeNPE;
        canAttackTimer = !freezeNPE;
        canSearchForTarget = !freezeNPE;
    }
    #region Getters
    public Entity GetTarget()
    {
        return target;
    }
    #endregion
}
