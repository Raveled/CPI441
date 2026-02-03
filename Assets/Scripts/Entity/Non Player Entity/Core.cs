using UnityEngine;
using System.Collections;

public class Core : NonPlayerEntity 
{
    [Header("Core Setup")]
    [SerializeField] Minion minionPrefab = null;
    [SerializeField] int numMinionsInWave = 5;
    [SerializeField] float timeBetweenMinionsInWave = 1f;
    [SerializeField] Transform[] waveSpawnOrigins = null;

    GameManager gameManager;
    protected override void Start() {
        gameManager = FindFirstObjectByType<GameManager>();
        base.Start();
    }
    protected override void Die(Entity damageOrigin) {
        gameManager.GameEnd(team);
        Destroy(gameObject);
    }
    public void SpawnWave() {
        if (isDead) return;
        StartCoroutine(SpawnMinion());
    }
    IEnumerator SpawnMinion() {
        //Spawn a minion every timeBetweenMinionsInWave seconds according to numMinionsInWave
        for(int i = 0; i < numMinionsInWave; i++) {
            foreach (Transform t in waveSpawnOrigins) {
                //spawn minion at all origin points
                Minion m = Instantiate(minionPrefab, t.position, t.rotation, t);
                m.SetTeam(team);
            }
            yield return new WaitForSeconds(timeBetweenMinionsInWave);
        }
    }


}
