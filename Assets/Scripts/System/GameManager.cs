using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public enum GameState : int { NULL = 0, INPROGRESS = 1, PAUSED = 2, END = 3}
    //[Header("GameManager Setup")]
    //[SerializeField] CreepSpawner[] creepSpawners = null; //Collected for map info
    //[Space]
    [Header("GameManager Debug")]
    [SerializeField] GameState gameState = GameState.NULL;
    [SerializeField] float gameTimer = 0;
    [SerializeField] string timerString = "";
    void Start()
    {
        gameState = GameState.INPROGRESS;
        gameTimer = 0;
    }

    void Update()
    {
        GameTimer();
    }
    //Handle the game timer
    void GameTimer() {

        if (gameState == GameState.INPROGRESS) {
            gameTimer += Time.deltaTime;
        }

        TimeSpan time = TimeSpan.FromSeconds(gameTimer);
        timerString = time.Minutes.ToString() + ":" + time.Seconds.ToString();
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
        if(gameState == GameState.INPROGRESS) { //Pause
            gameState = GameState.PAUSED;
        }else if(gameState == GameState.PAUSED) { //Unpause
            gameState = GameState.INPROGRESS;
        }
    }
}
