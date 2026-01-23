using UnityEngine;

public class Minion : NonPlayerEntity
{
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
        //WIP
        //A* pathfinding towards target
    }
    protected override void FindTarget() {
        //WIP
        if(!target) {
            /*
             * 1. tower
             * 2. minions
             * 3. players
             * 4. closest point on map
             */
        }
    }
    protected override void Attack() {
        //WIP
        if(target && CheckRangeToTarget()) {
            //attack
        }
    }
    //Make sure target is in range
    bool CheckRangeToTarget() {
        //WIP
        return true;
    }
}
