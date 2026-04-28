using UnityEngine;
using UnityEngine.TextCore.Text;
using System.Collections.Generic;
using PurrNet;
using NUnit.Framework;
using PurrNet.Prediction;

public class Tower : NonPlayerEntity
{
    [Header("Tower Setup")]
    [SerializeField] GameObject towerProjectilePrefab = null;
    [SerializeField] SyncVar<float> projectileSpeed = new(10f);
    [SerializeField] Transform projectileOrigin = null;
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

    protected override void ServerUpdate() {
        if (isDead) return;
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
            Vector3 direction = (currentTarget.transform.position - projectileOrigin.position).normalized;

            // Send ServerRpc to handle the projectile spawning
            ServerSpawnTowerProjectileRpc(projectileOrigin.position, attackRangeOrigin.rotation, direction, currentTarget);
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnTowerProjectileRpc(Vector3 projectileOrigin, Quaternion rotation, Vector3 direction, Entity target)
    {
        if (!isServer) return;

        ServerSpawnTowerProjectile(projectileOrigin, rotation, direction, target);
    }

    private void ServerSpawnTowerProjectile(Vector3 projectileOrigin, Quaternion rotation, Vector3 direction, Entity target)
    {
        if (towerProjectilePrefab == null) 
        { 
            Debug.LogError("[Tower] towerProjectilePrefab is NULL!"); 
            return; 
        }

        GameObject proj = Instantiate(towerProjectilePrefab, projectileOrigin, rotation);

        proj.GetComponent<TowerProjectile>().SpawnSetup(this, attackPower.value, direction, projectileSpeed.value, target);

        NetworkManager.main.Spawn(proj);
    }

    protected override void Die(Entity damageOrigin) {
        if (isServer)
        {
            FindFirstObjectByType<GameManager>().TowerDestroyed(GetTeam());
        }
        
        base.Die(damageOrigin);

        //REMOVE WHEN ANIMATION IS IN
        Destroy(gameObject);
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
