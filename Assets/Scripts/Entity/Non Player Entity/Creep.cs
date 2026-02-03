using UnityEngine;

public class Creep : NonPlayerEntity 
{
    [Header("Creep Setup")]
    [SerializeField] Transform patrolOrigin = null; //assigned by spawner
    [SerializeField] float patrolRange = 10f;
    [Space]
    [Header("Creep Debug")]
    [Tooltip("Blue Circle")]
    [SerializeField] bool showPatrolRange = false;
    [Space]
    [SerializeField] bool isActive = false;
    [SerializeField] CreepSpawner connectedSpawner = null;
    protected override void Start() {
        base.Start();
    }
    void Update()
    {
        Move();
        CheckPatrolRange();
        CheckTargetRange();
        AttackTimer();
        Attack();
    }
    //Continuously check to see if in patrol range
    void CheckPatrolRange() {
        //WIP-------------------------------------------------------------------------------------------------------
        if (Vector3.Distance(transform.position, patrolOrigin.position) > patrolRange) {
            //return to patrol
        }
    }
    //Check if target is still in range. If not, become inactive
    void CheckTargetRange() {
        if (target) {
            if (Vector3.Distance(attackRangeOrigin.position, target.transform.position) > attackRange) {
                target = null;
                isActive = false;
            }
        }
    }
    protected override void Move() {
        //WIP-------------------------------------------------------------------------------------------------------
        if (isActive && target) {
            //move
        }
    }
    protected override void Attack() {
        //WIP-------------------------------------------------------------------------------------------------------
        if (isActive && target && attackCooldownTimer <= 0) {
            //attack
        }
    }
    protected override void DistributeGoldReward() {
        //WIP-------------------------------------------------------------------------------------------------------

    }
    public override void TakeDamage(int damage, Entity damageOrigin) {
        //WIP-------------------------------------------------------------------------------------------------------
        //On Damage, become active with damager as the target
        isActive = true;
        if(damageOrigin is Entity) //THIS SHOULD BE PLAYER
        {
            target = damageOrigin;
        }

        base.TakeDamage(damage, damageOrigin);
    }
    protected override void Die(Entity damageOrigin) {
        //Lower activeCreepCount of connected CreepSpawner
        connectedSpawner.CreepDied();
        base.Die(damageOrigin);
    }
    protected override void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (showPatrolRange) {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(patrolOrigin.position, patrolRange);
        }
    }
    #region Setters
    public void SetPatrolOrigin(Transform origin) {
        patrolOrigin = origin;
    }
    public void SetConnectedSpawner(CreepSpawner spawner) {
        connectedSpawner = spawner;
    }
    #endregion
}
