using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.AI;
using NUnit.Framework;
using PurrNet;

public class Minion : NonPlayerEntity
{
    [Header("Minion Setup")]
    [SerializeField] float attackHeight = 10f;
    [SerializeField] bool attackIsAOE = false;

    [Header("Minion Debug")]
    [Tooltip("Purple Circle")]
    [SerializeField] bool showAttackBounds = false;
    [Space]
    [SerializeField] Transform navMeshMoveTarget = null;
    
    //Nav Mesh [SERVER CONTROLLED ONLY - CLIENTS SHOULD INTERP POS]
    [SerializeField] Core enemyCore = null;
    NavMeshAgent agent = null;
    float distanceToTarget = 0f;
    Vector3 previousDestination;

    //Attack Cylinder - Local Calculation for OverlapCapsule
    Vector3 p0 = new Vector3();
    Vector3 p1 = new Vector3();
    Vector3 halfHeight;

    //Distance Checking - Local Visualization
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

            if (isServer)
            {  
                agent.speed = moveSpeed;
                navMeshMoveTarget = enemyCore.transform;
                agent.SetDestination(enemyCore.transform.position);
                agent.isStopped = false;
                agent.updateRotation = true;
            }
            else agent.enabled = false;
        }
    }

    void Update()
    {
        if (isServer) ServerUpdate();

        ClientUpdate();
    }

    void ServerUpdate()
    {
        Entity currentTarget = GetTarget();
        if(hasTarget && !currentTarget) 
        {
            ResetTarget();
        }

        //currentTarget = FindTarget();

        FindTarget();
        currentTarget = GetTarget();

        if (enemyCore) 
        {
            Move(currentTarget);
            Rotate(currentTarget);
        }

        AttackTimer();
        Attack(currentTarget);
    }

    void ClientUpdate()
    {
        //Update the cylinder points for attack
        p0 = attackRangeOrigin.position + halfHeight;
        p1 = attackRangeOrigin.position - halfHeight;

        //Debug for showing attackrange distance as a line
        if (showAttackBounds) {
            Entity currentTarget = GetTarget();
            if (currentTarget)
            {
                if (CheckTargetInAttackRange(currentTarget)) {
                    debugLineColor = Color.green;
                } else {
                    debugLineColor = Color.red;
                }
                basePOSThis = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
                basePOSTarget = new Vector3(currentTarget.transform.position.x, 0, currentTarget.transform.position.z);
                Debug.DrawLine(basePOSThis, basePOSTarget, debugLineColor);
            }
        }
    }

    protected override void Move(Entity currentTarget) {
        base.Move();

        if (!isServer) return;

        //Set move target
        navMeshMoveTarget = currentTarget ? currentTarget.transform : enemyCore.transform;

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

    protected override void Attack(Entity currentTarget) {
        base.Attack();

        
        if (!isServer) return;



        //If there is a target and it is within attack range
        if (currentTarget && CheckTargetInAttackRange(currentTarget) && attackCooldownTimer.value <= 0) {

            if(currentTarget == this) {
                Debug.Log(gameObject.name + " is targeting self -- Attack()");

                //Debug.Log($"[Minion Attack] Attempting attack on target: {currentTarget?.name}");
            }


            //Spawn overlapcapsule and check for enemy entities to deal damage to
            Collider[] hits = Physics.OverlapCapsule(p0, p1, attackRange);

            foreach (Collider hit in hits) {
                //if collider is an entity on enemy team, deal damage to it
                if(hit.gameObject.TryGetComponent<Entity>(out Entity e)){
                    if (e.GetIsDead()) continue;

                    //If not AOE attack, only hit target
                    if (!attackIsAOE && e != currentTarget) continue;


                    //Deal Damage if on enemy team
                    if (GetEnemyTeams().Contains(e.GetTeam())) 
                    {
                        //Reset attack cooldown
                        attackCooldownTimer.value = defaultAttackCooldown.value;


                        if (e.TakeDamage(attackPower, this)) {
                            ResetTarget();
                        }
                    }
                    else
                    {
                        Debug.Log($"[REJECTED] {e.name} is a teammate. Search for a new target!");
                        ResetTarget(); // Force the minion to stop looking at its friend
                    }
                }
            }
        }
    }

    //Rotate the Transform based on the target
    void Rotate(Entity currentTarget) {
        if (!isServer) return;
        
        if (!currentTarget) {
            //If no target, use agent rotation
            agent.updateRotation = true;
        } else {
            //If target, use custom rotation
            agent.updateRotation = false;

            //Get rotate direction
            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0f; // keep upright

            if (direction.sqrMagnitude < 0.001f) return;

            //Rotate
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, agent.angularSpeed / 2 * Time.deltaTime);
        }
    }
    //Make sure target is in range
    bool CheckTargetInAttackRange(Entity currentTarget) {
        if (currentTarget) {
            //Flatten y to only check horizontal distance
            basePOSThis = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
            basePOSTarget = new Vector3(currentTarget.transform.position.x, 0, currentTarget.transform.position.z);
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
