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
        FindTarget();
        Move();
        AttackTimer();
        Attack();
    }
    protected override void Move() {
        //WIP-------------------------------------------------------------------------------------------------------
        //A* pathfinding towards target
    }
    protected override void FindTarget() {
        //WIP-------------------------------------------------------------------------------------------------------
        if (!target) {
            Entity[] entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
            
            foreach (Entity e in entities)
            {
                if (e.GetTeam() != enemyTeam) continue;
                float distance = Vector3.Distance(e.transform.position, transform.position);
                if(distance < minDistanceTower && distance < effectiveTargetRange)
                if (e is Tower)
                {
                    closestTower = e.transform;
                    minDistanceTower = distance;
                }else if (e is Minion)
                {
                    closestMinion = e.transform;
                    minDistanceMinion = distance;
                } else if (e is Entity) //THIS SHOULD BE PLAYER
                {
                    closestPlayer = e.transform;
                    minDistancePlayer = distance;
                }
            }

            if (closestTower) target = closestTower;
            else if (closestMinion) target = closestMinion;
            else if (closestPlayer) target = closestPlayer;
            else target = null; //THIS WOULD BE NEXT POINT ON PATH INSTEAD
        } else
        {
            ////Check to see if target is still in effective range
            //if (Vector3.Distance(transform.position, target.position) > effectiveTargetRange)
            //{
            //    target = null;
            //}
        }
    }
    protected override void Attack() {
        //WIP-------------------------------------------------------------------------------------------------------
        if (target && CheckRangeToTarget()) {
            //attack
        }
    }
    //Make sure target is in range
    bool CheckRangeToTarget() {
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
