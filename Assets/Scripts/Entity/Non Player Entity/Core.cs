using UnityEngine;
using System.Collections;
using PurrNet;
using System.Collections.Generic;

public class Core : NonPlayerEntity 
{
    [Header("Core Setup")]
    [SerializeField] Minion minionPrefab = null;
    [SerializeField] SO_EntityStatBlock minionStatblock = null;
    int numMinionsInWave = 5;
    [SerializeField] float timeBetweenMinionsInWave = 1f;
    [SerializeField] Transform[] waveSpawnOrigins = null;
    [SerializeField] List<Transform> waypoints_Path1 = new List<Transform>();
    [SerializeField] List<Transform> waypoints_Path2 = new List<Transform>();
    [SerializeField] Core enemyCore = null;
    bool canSpawnMinions = true;

    GameManager gameManager;
    protected override void Start() {
        gameManager = FindFirstObjectByType<GameManager>();
        base.Start();
    }
    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);

        if (!isServer) return; // Only execute end game logic on the server

        gameManager.GameEnd(team);
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

    //Setter
    public void SetMinionStatblock(SO_EntityStatBlock stats) {
        minionStatblock = stats;
    }
    public void SetNumMinionsInWave(int num) {
        numMinionsInWave = num;
    }



}
