using UnityEngine;
using System;
using Unity.AI.Navigation;

public class GameManager : MonoBehaviour
{
    public enum GameState : int { NULL = 0, INPROGRESS = 1, PAUSED = 2, END = 3}
    [Header("GameManager Setup")]
    [Tooltip("Time In Seconds")]
    [SerializeField] float minionWaveSpawnInterval = 5f;
    [SerializeField] NavMeshSurface navMeshSurface = null;
    [Tooltip("Set to -1 for infinite waves default")]
    [SerializeField] int maxWaves = -1;
    [Space]
    [SerializeField] bool spawnWaveOnStart = false;
    [Space]
    [Header("GameManager Debug")]
    [SerializeField] bool bakeOnStart = false;
    [Space]
    [SerializeField] GameState gameState = GameState.NULL;
    [SerializeField] string timerString = "";
    [Space]
    [Header("Minion Spawning Debug")]
    [SerializeField] float currentMinionWaveSpawnTimer = 0;
    
    //Game Timer
    float gameTimer = 0;
    
    //For wave spawning
    Core[] cores = null;
    int currentWaves = 0;
    void Start()
    {
        //Init
        gameState = GameState.INPROGRESS;
        gameTimer = 0;
        cores = FindObjectsByType<Core>(FindObjectsSortMode.None);
        currentMinionWaveSpawnTimer = minionWaveSpawnInterval;

        if(bakeOnStart) navMeshSurface.BuildNavMesh();
        if (spawnWaveOnStart) SpawnWave();
    }

    void Update()
    {
        //If game is in progress, perform these actions
        if(gameState == GameState.INPROGRESS) {
            GameTimer();
            MinionWaveSpawnTimer();
        }
    }
    //Handle the game timer
    void GameTimer() {
        //Update Timer
        gameTimer += Time.deltaTime;

        //Update timer visual
        TimeSpan time = TimeSpan.FromSeconds(gameTimer);
        timerString = time.Minutes.ToString() + ":" + time.Seconds.ToString();
    }
    //Handle minion wave spawn timer
    void MinionWaveSpawnTimer() {
        //If using max # of waves for debug
        if(maxWaves != -1) {
            if (currentWaves >= maxWaves) return;
        }

        //Update timer
        currentMinionWaveSpawnTimer -= Time.deltaTime;

        //If timer is 0 or below, spawn a new wave at each core and reset timer
        if (currentMinionWaveSpawnTimer <= 0) {
            currentMinionWaveSpawnTimer = minionWaveSpawnInterval;
            SpawnWave();
            currentWaves++;
        }
    }
    //Spawn a Wave
    void SpawnWave() {
        for (int i = 0; i < cores.Length; i++) {
            cores[i].SpawnWave();
        }
    }
    //When a core is destroyed, this will be called, ending the game
    public void CoreDestroyed(int team) {
        gameState = GameState.END;
        if(team == 1) {
            //team 2 wins
        }else if(team == 2) {
            //team 1 wins
        }
    }
    //Pause or unpause game
    public void TogglePauseGame() {
        if (gameState == GameState.INPROGRESS) { //Pause if unpaused
            gameState = GameState.PAUSED;
        } else if (gameState == GameState.PAUSED) { //Unpause if paused
            gameState = GameState.INPROGRESS;
        }
    }
}
