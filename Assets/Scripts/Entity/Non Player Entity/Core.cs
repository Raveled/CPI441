using UnityEngine;
using System.Collections;
using PurrNet;
using System.Collections.Generic;

public class Core : NonPlayerEntity 
{
    [Header("Core Setup")]
    [SerializeField] Minion minionPrefab = null;
    [SerializeField] SO_EntityStatBlock minionStatblock = null;
    [SerializeField] float rotateAngularSpeed = 10f;
    int numMinionsInWave = 5;
    [SerializeField] float timeBetweenMinionsInWave = 1f;
    [SerializeField] Transform[] waveSpawnOrigins = null;
    [SerializeField] List<Transform> waypoints_Path1 = new List<Transform>();
    [SerializeField] List<Transform> waypoints_Path2 = new List<Transform>();
    [SerializeField] Core enemyCore = null;
    bool canSpawnMinions = true;

    Vector3 basePOSThis;
    Vector3 basePOSTarget;

    GameManager gameManager;
    protected override void Start() {
        gameManager = FindFirstObjectByType<GameManager>();
        base.Start();
    }
    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);

        if (!isServer) return; // Only execute end game logic on the server

        gameManager.GameEnd(team);

        //REMOVE WHEN ANIMATION IS IN
        Destroy(gameObject);
    }
    //Start the spawning of a minion wave
    public void SpawnWave() {
        if (!canSpawnMinions) return;
        if (!isServer) return; // Only server spawns minions
        
        if (GetIsDead()) return;

        StartCoroutine(SpawnMinion());
    }

    //Spawn the minion in waves
    IEnumerator SpawnMinion() {
        if (!isServer) yield break; // Only server spawns minions

        //Spawn a minion every timeBetweenMinionsInWave seconds according to numMinionsInWave
        for(int i = 0; i < numMinionsInWave; i++) {
            if (!canSpawnMinions) yield break;
            if (isDead) yield break;

            for(int j = 0; j < waveSpawnOrigins.Length; j++) {
                Minion m = Instantiate(minionPrefab, waveSpawnOrigins[j].position, waveSpawnOrigins[j].rotation, waveSpawnOrigins[j]);
                m.SetTeam(team);
                m.SetStatblock(minionStatblock);
                m.SetEnemyCore(enemyCore);

                //Path 1 is the one spawning from waveSpawnOrigins[0]
                var x = j == 0 ? waypoints_Path1 : waypoints_Path2;
                m.SetWaypoints(x);
            }
            yield return new WaitForSeconds(timeBetweenMinionsInWave);
        }
    }
    public override void Freeze(bool freezeNPE) {
        base.Freeze(freezeNPE);
        canSpawnMinions = !freezeNPE;
    }
    protected override void ServerUpdate() {
        Entity currentTarget = GetTarget();
        if (hasTarget && !currentTarget) {
            ResetTarget();
        }

        if (enemyCore) {
            if (isDead) return;
            FindTarget();
            currentTarget = GetTarget();
            Rotate(currentTarget);
            AttackTimer();
            Attack(currentTarget);
        }

        base.ServerUpdate();
    }
    protected override void ClientUpdate() {
        base.ClientUpdate();
    }
    protected override void Attack(Entity currentTarget) {
        base.Attack();

        if (!isServer) return;

        //If there is a target and it is within attack range
        if (currentTarget && CheckTargetInAttackRange(currentTarget) && attackCooldownTimer.value <= 0) {

            if (currentTarget == this) {
                Debug.Log(gameObject.name + " is targeting self -- Attack()");
            }

            animator.SetTrigger("Attack");
            //deal dmg directly to target
            currentTarget.TakeDamage(attackPower, this);
        }
    }
    bool CheckTargetInAttackRange(Entity currentTarget) {
        if (currentTarget) {
            //Flatten y to only check horizontal distance
            basePOSThis = new Vector3(attackRangeOrigin.position.x, 0, attackRangeOrigin.position.z);
            basePOSTarget = new Vector3(currentTarget.transform.position.x, 0, currentTarget.transform.position.z);
            if (Vector3.Distance(basePOSTarget, basePOSThis) <= attackRange) return true;
        }
        return false;
    }
    void Rotate(Entity currentTarget) {
        if (!isServer) return;

        if (currentTarget) {
            //Get rotate direction
            Vector3 direction = currentTarget.transform.position - transform.position;
            direction.y = 0f; // keep upright

            if (direction.sqrMagnitude < 0.001f) return;

            //Rotate
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateAngularSpeed / 2 * Time.deltaTime);
        }
    }
    //Setter
    public void SetMinionStatblock(SO_EntityStatBlock stats) {
        minionStatblock = stats;
    }
    public void SetNumMinionsInWave(int num) {
        numMinionsInWave = num;
    }



}
