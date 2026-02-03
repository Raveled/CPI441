using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Collections.Generic;

public class Tower : NonPlayerEntity
{
    [Header("Tower Setup")]
    [SerializeField] TowerProjectile towerProjectilePrefab = null;
    [SerializeField] float projectileSpeed = 5f;
    protected override void Start() {
        base.Start();
    }

    void Update() {
        FindTarget();
        Attack();
        AttackTimer();
    }
    protected override void Attack() {
        base.Attack();
        //If there is a current target and the attack cooldown is ready
        if (target && attackCooldownTimer <= 0) {
            //Reset attack cooldown
            attackCooldownTimer = defaultAttackCooldown;

            //Get direction between target and this tower
            Vector3 direction = (target.transform.position - attackRangeOrigin.position).normalized;

            //Instantiate new projectile and set it's properties
            TowerProjectile proj = Instantiate(towerProjectilePrefab, attackRangeOrigin.position, attackRangeOrigin.rotation, attackRangeOrigin);
            proj.SpawnSetup(this, attackPower, direction, projectileSpeed, target);
        }
    }
    //When a player attacks another player within the range of the tower
    public void OverrideTarget(Entity damageOrigin) {
        //WIP-------------------------------------------------------------------------------------------------------
        //Check if Enemy player that dealt the damage is within range of this tower
        float distance = Vector3.Distance(attackRangeOrigin.position, damageOrigin.gameObject.transform.position);
        if(distance < attackRange)
        {
            //Set the target of this tower to the enemy player that dealt damage
            target = damageOrigin;
        }
    }
}
