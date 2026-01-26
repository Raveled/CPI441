using UnityEngine;
using UnityEngine.UIElements;

public class Minion : NonPlayerEntity
{
    //Fields
    Transform closestTower = null;
    float minDistanceTower = Mathf.Infinity;
    Transform closestMinion = null;
    float minDistanceMinion = Mathf.Infinity;
    Transform closestPlayer = null;
    float minDistancePlayer = Mathf.Infinity;

    [Header("Minion Setup")]
    [Tooltip("If multiple types of Entity in this range, will choose in order: Tower, Minion, Player")]
    [SerializeField] float effectiveTargetRange = 5f;
    [Space]
    [Header("Minion Debug")]
    [Tooltip("Purple Circle")]
    [SerializeField] bool showEffectiveTargetRange = false;


    protected override void Start()
    {
        base.Start();
    }

    void Update()
    {
        //FindTarget();
        Move();
        AttackTimer();
        Attack();
    }
    protected override void Move() {
        //WIP-------------------------------------------------------------------------------------------------------
        //A* pathfinding towards target
    }
    //protected override void FindTarget() {
    //    //WIP-------------------------------------------------------------------------------------------------------
    //    //Only check if theres no current target
    //    if (!target) {
    //        //Get all possible entities
    //        Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);

    //        foreach (Entity e in entities) {
    //            //If entity is not on the enemy team, ignore
    //            if (e.GetTeam() != enemyTeam) continue;

    //            //For Tower, Minion, Player, check for the closest ones
    //            float distance = Vector3.Distance(e.transform.position, transform.position);
    //            if (e is Tower && distance < minDistanceTower && distance < effectiveTargetRange) {
    //                closestTower = e.transform;
    //                minDistanceTower = distance;
    //            } else if (e is Minion && distance < minDistanceMinion && distance < effectiveTargetRange) {
    //                closestMinion = e.transform;
    //                minDistanceMinion = distance;
    //            } else if (e is Entity && distance < minDistancePlayer && distance < effectiveTargetRange) { //THIS SHOULD BE PLAYER
    //                closestPlayer = e.transform;
    //                minDistancePlayer = distance;
    //            }   
    //        }

    //        //In order of Tower > Minion > Player > None, set the target
    //        if (closestTower) target = closestTower;
    //        else if (closestMinion) target = closestMinion;
    //        else if (closestPlayer) target = closestPlayer;
    //        else target = null; //THIS WOULD BE NEXT POINT ON PATH INSTEAD
    //    } else
    //    {
    //        //Check to see if target is still in effective range
    //        if (Vector3.Distance(transform.position, target.position) > effectiveTargetRange) {
    //            target = null;
    //        }
    //    }
    //}
    protected override void Attack() {
        //WIP-------------------------------------------------------------------------------------------------------
        if (target && CheckTargetInAttackRange()) {
            //attack
        }
    }
    //Make sure target is in range
    bool CheckTargetInAttackRange() {
        if (Vector3.Distance(attackRangeOrigin.position, target.position) < attackRange) return true;
        return false;
    }
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (showEffectiveTargetRange)
        {
            Gizmos.color = Color.purple;
            Gizmos.DrawWireSphere(transform.position, effectiveTargetRange);
        }
    }
}
