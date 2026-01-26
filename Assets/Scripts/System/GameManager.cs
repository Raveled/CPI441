using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public enum GameState : int { NULL = 0, INPROGRESS = 1, PAUSED = 2, END = 3}
    [Header("GameManager Setup")]
    [Tooltip("Time In Seconds")]
    [SerializeField] float minionWaveSpawnInterval = 5f;
    //[SerializeField] CreepSpawner[] creepSpawners = null; //Collected for map info
    [Space]
    [Header("GameState Debug")]
    [SerializeField] GameState gameState = GameState.NULL;
    [SerializeField] float gameTimer = 0;
    [SerializeField] string timerString = "";
    [Space]
    [Header("Minion Spawning Debug")]
    [SerializeField] float currentMinionWaveSpawnTimer = 0;
    [SerializeField] Core[] cores = null;
    void Start()
    {
        //Init
        gameState = GameState.INPROGRESS;
        gameTimer = 0;
        cores = FindObjectsByType<Core>(FindObjectsSortMode.None);
        currentMinionWaveSpawnTimer = minionWaveSpawnInterval;
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
        //Update timer
        currentMinionWaveSpawnTimer -= Time.deltaTime;

        //If timer is 0 or below, spawn a new wave at each core and reset timer
        if (currentMinionWaveSpawnTimer <= 0) {
            currentMinionWaveSpawnTimer = minionWaveSpawnInterval;
            for(int i = 0; i < cores.Length; i++) {
                cores[i].SpawnWave();
            }
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
