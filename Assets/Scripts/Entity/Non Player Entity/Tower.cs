using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Collections.Generic;
using PurrNet;
using NUnit.Framework;

public class Tower : NonPlayerEntity
{
    [Header("Tower Setup")]
    [SerializeField] TowerProjectile towerProjectilePrefab = null;
    [SerializeField] SyncVar<float> projectileSpeed = new(5f);
    protected override void Start() {
        base.Start();
    }

    void Update()
    {
        if (isServer)
        {
            ServerUpdate();
        }
    }

    void ServerUpdate() {
        FindTarget();
        Attack();
        AttackTimer();
    }

    protected override void Attack() {
        base.Attack();

        if (!isServer) return;

        Entity currentTarget = GetTarget();

        //If there is a current target and the attack cooldown is ready
        if (currentTarget && attackCooldownTimer.value <= 0) {
            //Reset attack cooldown
            attackCooldownTimer.value = defaultAttackCooldown.value;

            //Get direction between target and this tower
            Vector3 direction = (currentTarget.transform.position - attackRangeOrigin.position).normalized;

            //Instantiate new projectile and set it's properties
            TowerProjectile proj = Instantiate(towerProjectilePrefab, attackRangeOrigin.position, attackRangeOrigin.rotation, attackRangeOrigin);
            proj.SpawnSetup(this, attackPower.value, direction, projectileSpeed.value, currentTarget);
        }
    }
    protected override void Die(Entity damageOrigin) {
        if (isServer)
        {
            FindFirstObjectByType<GameManager>().TowerDestroyed(GetTeam());
        }
        
        base.Die(damageOrigin);
    }
    //When a player attacks another player within the range of the tower
    public void OverrideTarget(Entity damageOrigin) {
        //WIP-------------------------------------------------------------------------------------------------------
        if (!isServer) return;

        if (damageOrigin == null || damageOrigin.GetIsDead())
            return;
            
        //Check if Enemy player that dealt the damage is within range of this tower
        float distance = Vector3.Distance(attackRangeOrigin.position, damageOrigin.gameObject.transform.position);
        if(distance < attackRange)
        {
            //Set the target of this tower to the enemy player that dealt damage
            SetTarget(damageOrigin);
        }
    }
}
