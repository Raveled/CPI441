using UnityEngine;
using System.Collections;
using PurrNet;

public class Core : NonPlayerEntity 
{
    [Header("Core Setup")]
    [SerializeField] Minion minionPrefab = null;
    [SerializeField] SO_EntityStatBlock minionStatblock = null;
    [SerializeField] int numMinionsInWave = 5;
    [SerializeField] float timeBetweenMinionsInWave = 1f;
    [SerializeField] Transform[] waveSpawnOrigins = null;

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
        if (!isServer) return; // Only server spawns minions
        
        if (GetIsDead()) return;

        StartCoroutine(SpawnMinion());
    }

    //Spawn the minion in waves
    IEnumerator SpawnMinion() {
        if (!isServer) yield break; // Only server spawns minions

        //Spawn a minion every timeBetweenMinionsInWave seconds according to numMinionsInWave
        for(int i = 0; i < numMinionsInWave; i++) {
            foreach (Transform t in waveSpawnOrigins) {
                //spawn minion at all origin points
                Minion m = Instantiate(minionPrefab, t.position, t.rotation, t);
                m.SetStatblock(minionStatblock);
                m.SetTeam(team);
            }
            yield return new WaitForSeconds(timeBetweenMinionsInWave);
        }
    }

    //Setter
    public void SetMinionStatblock(SO_EntityStatBlock stats) {
        minionStatblock = stats;
    }


}
