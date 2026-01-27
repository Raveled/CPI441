using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class Minion : NonPlayerEntity
{
    [Header("Minion Setup")]
    [SerializeField] float attackHeight = 10f;
    [Header("Minion Debug")]
    [Tooltip("Purple Circle")]
    [SerializeField] bool showAttackBounds = false;
    Vector3 p0 = new Vector3();
    Vector3 p1 = new Vector3();
    Vector3 halfHeight;
    public enum MinionState : int { FOLLOW_ENEMY = 0, FOLLOW_CORE = 1};
    MinionState minionState = MinionState.FOLLOW_CORE;

    [SerializeField] Transform followObject = null;

    protected override void Start()
    {
        base.Start();

        //Set the half height of attack cylinder
        halfHeight = Vector3.up * (attackHeight / 2f);

    }
    void Update()
    {
        FindTarget();
        Move();
        AttackTimer();
        Attack();

        if (target) minionState = MinionState.FOLLOW_ENEMY;

        //Update the cylinder points for attack
        p0 = attackRangeOrigin.position + halfHeight;
        p1 = attackRangeOrigin.position - halfHeight;

        //Debug for showing attackrange distance as a line
        if (showAttackBounds && target) {
            if (CheckTargetInAttackRange()) {
                Vector3 thisbasepos = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
                Vector3 targetbasepos = new Vector3(target.transform.position.x, 0, target.transform.position.z);
                Debug.DrawLine(targetbasepos, thisbasepos, Color.green);
            } else {
                Vector3 thisbasepos = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
                Vector3 targetbasepos = new Vector3(target.transform.position.x, 0, target.transform.position.z);
                Debug.DrawLine(targetbasepos, thisbasepos, Color.red);
            }
        }
    }
    protected override void FindTarget() {
        if(minionState == MinionState.FOLLOW_CORE || !target) {
            base.FindTarget();
        }
    }
    protected override void Move() {
        base.Move();
        //WIP-------------------------------------------------------------------------------------------------------
        //A* pathfinding towards target

        if (target) { //Move towards target

        } else { //Move towards waypoint

        }
    }
    protected override void Attack() {
        base.Attack();
        //If there is a target and it is within attack range
        if (target && CheckTargetInAttackRange() && attackCooldownTimer <= 0) {
            Debug.Log(GetName() + " is attacking " + target.GetName());
            //Reset attack cooldown
            attackCooldownTimer = defaultAttackCooldown;

            //Spawn overlapsphere and check for enemy entities to deal damage to
            //Collider[] hits = Physics.OverlapSphere(attackRangeOrigin.position, attackSphereRadius);


            Collider[] hits = Physics.OverlapCapsule(p0, p1, attackRange);


            foreach (Collider hit in hits) {
                //if collider is an entity on enemy team, deal damage to it
                if(hit.gameObject.TryGetComponent<Entity>(out Entity e)){
                    if (GetEnemyTeams().Contains(e.GetTeam())) {
                        e.TakeDamage(attackPower, this); 
                        Debug.Log(GetName() + " dealt damage to " + e.GetName());
                    }
                }
            }
        }
    }
    //Make sure target is in range
    bool CheckTargetInAttackRange() {
        if (target) {
            Vector3 thisbasepos = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
            Vector3 targetbasepos = new Vector3(target.transform.position.x, 0, target.transform.position.z);
            if (Vector3.Distance(targetbasepos, thisbasepos) <= attackRange) return true;
        }
        return false;
    }
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (showAttackBounds)
        {
            Gizmos.color = Color.purple;

            Gizmos.DrawWireSphere(p0, attackRange);
            Gizmos.DrawWireSphere(p1, attackRange);

            // Connect the sides
            Gizmos.DrawLine(p0 + Vector3.right * attackRange,
                            p1 + Vector3.right * attackRange);
            Gizmos.DrawLine(p0 - Vector3.right * attackRange,
                            p1 - Vector3.right * attackRange);
            Gizmos.DrawLine(p0 + Vector3.forward * attackRange,
                            p1 + Vector3.forward * attackRange);
            Gizmos.DrawLine(p0 - Vector3.forward * attackRange,
                            p1 - Vector3.forward * attackRange);
        }
    }
}
