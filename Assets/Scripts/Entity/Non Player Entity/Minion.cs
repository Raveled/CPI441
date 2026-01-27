using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.AI;

public class Minion : NonPlayerEntity
{
    public enum MinionState : int { CHASE_ENEMY = 0, CHASE_CORE = 1};
    [Header("Minion Setup")]
    [SerializeField] float attackHeight = 10f;
    [Header("Minion Debug")]
    [Tooltip("Purple Circle")]
    [SerializeField] bool showAttackBounds = false;
    [Space]
    [SerializeField] Transform navMeshMoveTarget = null;
    
    //Nav Mesh
    [SerializeField] Core enemyCore = null;
    NavMeshAgent agent = null;
    float distanceToTarget = 0f;
    Vector3 previousDestination;

    //Attack Cylinder
    Vector3 p0 = new Vector3();
    Vector3 p1 = new Vector3();
    Vector3 halfHeight;

    //Distance Checking
    Vector3 basePOSThis;
    Vector3 basePOSTarget;
    Color debugLineColor;

    protected override void Start()
    {
        base.Start();

        //Set the half height of attack cylinder
        halfHeight = Vector3.up * (attackHeight / 2f);

        //Set Enemy Core
        Core[] cores = FindObjectsByType<Core>(FindObjectsSortMode.None);
        foreach(Core c in cores) {
            if (GetEnemyTeams().Contains(c.GetTeam())) enemyCore = c;
        }

        if (enemyCore) {
            //NavMesh Init
            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            navMeshMoveTarget = enemyCore.transform;
            agent.SetDestination(enemyCore.transform.position);
            agent.isStopped = false;
        }
    }
    void Update()
    {
        FindTarget();
        if(enemyCore) Move();
        AttackTimer();
        Attack();

        //Update the cylinder points for attack
        p0 = attackRangeOrigin.position + halfHeight;
        p1 = attackRangeOrigin.position - halfHeight;

        //Debug for showing attackrange distance as a line
        if (showAttackBounds && target) {
            if (CheckTargetInAttackRange()) {
                debugLineColor = Color.green;
            } else {
                debugLineColor = Color.red;
            }
            basePOSThis = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
            basePOSTarget = new Vector3(target.transform.position.x, 0, target.transform.position.z);
            Debug.DrawLine(basePOSThis, basePOSTarget, debugLineColor);
        }
    }
    protected override void Move() {
        base.Move();

        //Set move target
        navMeshMoveTarget = target ? target.transform : enemyCore.transform;

        //Get distance to target
        distanceToTarget = Vector3.Distance(navMeshMoveTarget.position, transform.position);

        // Stop when in attack range AND we have a real target
        if (distanceToTarget <= attackRange) {
            agent.isStopped = true;
            return;
        }

        //Continue moving
        agent.isStopped = false;

        // Update path only if needed
        if (!agent.hasPath || Vector3.Distance(previousDestination, navMeshMoveTarget.position) > 0.5f) {
            previousDestination = navMeshMoveTarget.position;
            agent.SetDestination(previousDestination);
        }
    }
    protected override void Attack() {
        base.Attack();
        //If there is a target and it is within attack range
        if (target && CheckTargetInAttackRange() && attackCooldownTimer <= 0) {
            Debug.Log(GetName() + " is attacking " + target.GetName());
            //Reset attack cooldown
            attackCooldownTimer = defaultAttackCooldown;

            //Spawn overlapcapsule and check for enemy entities to deal damage to
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
            //Flatten y to only check horizontal distance
            basePOSThis = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
            basePOSTarget = new Vector3(target.transform.position.x, 0, target.transform.position.z);
            if (Vector3.Distance(basePOSTarget, basePOSThis) <= attackRange) return true;
        }
        return false;
    }
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (showAttackBounds)
        {
            Gizmos.color = Color.purple;

            //Draw Cylinder
            Gizmos.DrawWireSphere(p0, attackRange);
            Gizmos.DrawWireSphere(p1, attackRange);
            Gizmos.DrawLine(p0 + Vector3.right * attackRange, p1 + Vector3.right * attackRange);
            Gizmos.DrawLine(p0 - Vector3.right * attackRange, p1 - Vector3.right * attackRange);
            Gizmos.DrawLine(p0 + Vector3.forward * attackRange, p1 + Vector3.forward * attackRange);
            Gizmos.DrawLine(p0 - Vector3.forward * attackRange, p1 - Vector3.forward * attackRange);
        }
    }
}
