using UnityEngine;
using System;
using Unity.AI.Navigation;
using TMPro;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public enum GameState : int { NULL = 0, INPROGRESS = 1, PAUSED = 2, END = 3}

    //GameManager
    [Header("GameManager Setup")]
    [SerializeField] Transform[] spawnpoints_Team1 = null;
    [SerializeField] Transform[] spawnpoints_Team2 = null;
    [Tooltip("Time In Seconds")][SerializeField] float minionWaveSpawnInterval = 5f;
    [SerializeField] NavMeshSurface navMeshSurface = null;
    [Tooltip("Set to -1 for infinite waves default")][SerializeField] int maxWaves = -1;
    [Space]
    [SerializeField] bool spawnWaveOnStart = false;
    [SerializeField] bool spawnCharactersOnStart = false;
    [Space]
    [Header("GameManager Debug")]
    [SerializeField] bool bakeOnStart = false;
    [Space]
    [SerializeField] GameState gameState = GameState.NULL;
    [SerializeField] string timerString = "";

    //Character Handling
    [Space]
    [Header("Characters Setup")]
    [Tooltip("0=NULL;1=Beetle;2=Mosquito;3=Butterfly")]
    [SerializeField] int[] chosenCharacters_Team1 = null;
    [Tooltip("0=NULL;1=Beetle;2=Mosquito;3=Butterfly")]
    [SerializeField] int[] chosenCharacters_Team2 = null;
    [Space]
    [SerializeField] GameObject beetle_Prefab = null;
    [SerializeField] GameObject mosquito_Prefab = null;
    [SerializeField] GameObject butterfly_Prefab = null;

    //Minion Spawning
    [Space]
    [Header("Minion Spawning Debug")]
    [SerializeField] float currentMinionWaveSpawnTimer = 0;

    //Pausing
    [Space]
    [Header("Pausing Debug")]
    [SerializeField] bool canPause = true;
    
    //Game Timer
    float gameTimer = 0;
    
    //For wave spawning/init
    Core[] cores = null;
    Tower[] towers = null;
    int currentWaves = 0;

    //Players
    [Space]
    [Header("Player/Team Setup")]
    [SerializeField] int teamSize = 3;
    [Space]
    [Header("Player/Team Debug")]
    [SerializeField] Player[] team1_Players = null;
    [SerializeField] Player[] team2_Players = null;

    //JSON
    [Header("JSON Setup")]
    JSON_MatchData matchData = null;
    JSON_EntityData entityData = null;
    [SerializeField] SO_EntityStatBlock stats_Core;
    [SerializeField] SO_EntityStatBlock stats_Minion;
    [SerializeField] SO_EntityStatBlock stats_Tower;
    [SerializeField] SO_EntityStatBlock stats_Char_Beetle;
    [SerializeField] SO_EntityStatBlock stats_Char_Mosquito;
    [SerializeField] SO_EntityStatBlock stats_Char_Butterfly;
    [Space]
    [Header("JSON Debug")]
    [SerializeField] bool LoadJSONOnStart = false;
    [Space]
    [SerializeField] int match_id = 0;
    [SerializeField] string timestamp = "";
    [SerializeField] string mapName = "m_TwoLane";
    [SerializeField] string winnerStr = "";
    [SerializeField] int towersDestroyed_Team1 = 0; //how many of Team 2's towers Team 1 destroyed
    [SerializeField] int towersDestroyed_Team2 = 0;


    private void Awake() {
        //Load in the stats from the JSON
        matchData = GetComponent<JSON_MatchData>();
        entityData = GetComponent<JSON_EntityData>();
        if (LoadJSONOnStart)LoadEntityJSON();
    }
    void Start() {
        //Init
        gameState = GameState.INPROGRESS;
        gameTimer = 0;
        currentMinionWaveSpawnTimer = minionWaveSpawnInterval;

        //Setup Cores+Towers
        cores = FindObjectsByType<Core>(FindObjectsSortMode.None);
        foreach (Core c in cores) {
            c.SetStatblock(stats_Core);
            c.SetMinionStatblock(stats_Minion);
        }
        towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
        foreach (Tower t in towers) t.SetStatblock(stats_Tower);

        //NavMesh
        if (bakeOnStart) navMeshSurface.BuildNavMesh();

        //Spawning
        if (spawnWaveOnStart) SpawnWave();
        if(spawnCharactersOnStart) SpawnCharacters();

        //Get All Players
        team1_Players = new Player[teamSize];
        team2_Players = new Player[teamSize];
        Player[] getPlayers = FindObjectsByType<Player>(FindObjectsSortMode.None);
        int t1Idx = 0;
        int t2Idx = 0;
        foreach(Player p in getPlayers) {
            if(p.GetTeam() == Entity.Team.TEAM1) {
                team1_Players[t1Idx] = p;
                t1Idx++;
            }
            else if (p.GetTeam() == Entity.Team.TEAM2) {
                team2_Players[t2Idx] = p;
                t2Idx++;
            }
        }
    }

    void Update()
    {
        //If game is in progress, perform these actions
        if(gameState == GameState.INPROGRESS) {
            GameTimer();
            MinionWaveSpawnTimer();
        }

        //Debug
        if (canPause) {
            if (Keyboard.current.pKey.wasPressedThisFrame) {
                TogglePauseGame();
            }
        }
        if (Keyboard.current.lKey.wasPressedThisFrame) {
            GenerateMatchJSON();
        }
        if (Keyboard.current.kKey.wasPressedThisFrame) {
            LoadMatchJSON();
        }
        if (Keyboard.current.mKey.wasPressedThisFrame) {
            GenerateEntityJSON();
        }
        if (Keyboard.current.nKey.wasPressedThisFrame) {
            LoadEntityJSON();
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
    //Spawn Characters
    void SpawnCharacters() {
        for(int i = 0; i < spawnpoints_Team1.Length; i++) {
            if (i < chosenCharacters_Team1.Length) {
                if (chosenCharacters_Team1[i] != 0) {
                    SpawnCharacter(chosenCharacters_Team1[i], spawnpoints_Team1[i]);
                }
            }
            if(i < chosenCharacters_Team2.Length) {
                if (chosenCharacters_Team2[i] != 0) {
                    SpawnCharacter(chosenCharacters_Team2[i], spawnpoints_Team2[i]);
                }
            }
        }
    }
    //Spawn a character at a spawnpoint
    void SpawnCharacter(int characterType, Transform spawnpoint) {
        GameObject character = null;
        SO_EntityStatBlock stats = null;

        switch (characterType) {
            case 0: // NULL
                break;
            case 1: //Beetle
                character = beetle_Prefab;
                stats = stats_Char_Beetle;
                break;
            case 2: //Mosquito
                character = mosquito_Prefab;
                stats = stats_Char_Mosquito;
                break;
            case 3: //Butterfly
                character = butterfly_Prefab;
                stats = stats_Char_Butterfly;
                break;
        }
        GameObject c = Instantiate(character, spawnpoint.position, spawnpoint.rotation, spawnpoint);
        Player p = c.GetComponent<Player>();
        p.SetStatblock(stats);
    }
    //When a core is destroyed, this will be called, ending the game
    public void GameEnd(Entity.Team team) {
        if(team == Entity.Team.TEAM1) {
            winnerStr = "Team1";
        }else if(team == Entity.Team.TEAM2) {
            winnerStr = "Team2";
        }
        ChangeGameState(GameState.END);
        GenerateMatchJSON();
    }

    //When a Tower is destroyed, this is called for JSON
    public void TowerDestroyed(Entity.Team team) {
        //WIP-----------------------------------------------------------
        //announce to game
        if(team == Entity.Team.TEAM1) {
            towersDestroyed_Team2++;
        }else if(team == Entity.Team.TEAM2) {
            towersDestroyed_Team1++;
        }
    }
    //Pause or unpause game
    public void TogglePauseGame() {
        if (gameState == GameState.INPROGRESS) { //Pause if unpaused
            ChangeGameState(GameState.PAUSED);
        } else if (gameState == GameState.PAUSED) { //Unpause if paused
            ChangeGameState(GameState.INPROGRESS);
        }
    }
    //Change Game State
    void ChangeGameState(GameState state) {
        switch (state) {
            case GameState.NULL:
                gameState = GameState.NULL;
                break;
            case GameState.INPROGRESS:
                gameState = GameState.INPROGRESS;
                FreezeAllNPE(false);
                FreezeAllPCs(false);
                break;
            case GameState.PAUSED:
                gameState = GameState.PAUSED;
                FreezeAllNPE(true);
                FreezeAllPCs(true);
                break;
            case GameState.END:
                gameState = GameState.END;
                FreezeAllNPE(true);
                ShowEndGameVisual();
                break;
        }
    }
    //Stop all enemy movement/attacking/target searching
    void FreezeAllNPE(bool freeze) {
        //WIP--------------------------------------------------------------
        NonPlayerEntity[] NPEs = FindObjectsByType<NonPlayerEntity>(FindObjectsSortMode.None);

        //Loop through all NPEs and freeze them
        foreach (NonPlayerEntity e in NPEs) {
            e.Freeze(freeze);
        }
    }
    #region JSON
    //Get A Team's total killcount
    public int ComputeTeamKillTotal(Player[] team) {
        int killCount = 0;
        for(int i = 0; i < team.Length; i++) {
            if (!team[i]) continue;
            killCount += team[i].GetPlayerInfo().KillCount;
        }
        return killCount;
    }
    public int ComputeTeamDeathTotal(Player[] team) {
        int deathCount = 0;
        for (int i = 0; i < team.Length; i++) {
            if (!team[i]) continue;
            deathCount += team[i].GetPlayerInfo().DeathCount;
        }
        return deathCount;
    }
    void GenerateMatchJSON() {
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        matchData.ImportMatchData(
            match_id,
            timestamp,
            mapName,
            winnerStr,
            (int)gameTimer,
            ComputeTeamKillTotal(team1_Players),
            ComputeTeamKillTotal(team2_Players),
            ComputeTeamDeathTotal(team1_Players),
            ComputeTeamDeathTotal(team2_Players),
            towersDestroyed_Team1,
            towersDestroyed_Team2
            );
        matchData.SaveToJSON();
    }
    void LoadMatchJSON() {
        matchData.LoadFromFile();
        Match m = matchData.GetMatch();

        match_id = m.match_id;
        timestamp = m.timestamp;
        mapName = m.map;
        winnerStr = m.winner;
        towersDestroyed_Team1 = m.towers_destroyed["Team1"];
        towersDestroyed_Team2 = m.towers_destroyed["Team2"];
    }
    void GenerateEntityJSON() {
        entityData.ImportEntityData(
            stats_Core,
            stats_Minion,
            stats_Tower,
            stats_Char_Beetle,
            stats_Char_Mosquito,
            stats_Char_Butterfly
            );
        entityData.SaveToJSON();
    }
    void LoadEntityJSON() {
        entityData.LoadFromFile();
        Statblocks statblocks = entityData.GetStatblocks();

        #region NPEs
        LoadEntityHelper(stats_Core, "Core", statblocks);
        LoadEntityHelper(stats_Minion, "Minion", statblocks);
        LoadEntityHelper(stats_Tower, "Tower", statblocks);
        #endregion
        #region PCs
        LoadEntityHelper(stats_Char_Beetle, "Beetle", statblocks);
        LoadEntityHelper(stats_Char_Mosquito, "Mosquito", statblocks);
        LoadEntityHelper(stats_Char_Butterfly, "Butterfly", statblocks);
        #endregion
    }
    //Helper function for loading data
    void LoadEntityHelper(SO_EntityStatBlock statBlock, string entityType, Statblocks loadFrom) {
        statBlock.BaseHitPoints = loadFrom.entityStats[entityType].baseHitPoints;
        statBlock.BaseMoveSpeed = loadFrom.entityStats[entityType].baseMoveSpeed;
        statBlock.BaseAttackPower = loadFrom.entityStats[entityType].baseAttackPower;
        statBlock.BaseDefaultAttackCooldown = loadFrom.entityStats[entityType].baseDefaultAttackCooldown;
        statBlock.RewardGold = loadFrom.entityStats[entityType].rewardGold;
        statBlock.RewardXP = loadFrom.entityStats[entityType].rewardXP;
    }
    #endregion
    #region WORK IN PROGRESS
    void FreezeAllPCs(bool freeze) {
        //WIP---------------------------------------------------------------------
    }
    void ShowEndGameVisual() {
        //WIP---------------------------------------------------------------------
        //for each player, display their end game visual with win/lose respectively
    }
    //When a Player Dies (Called by player death) ***Uses playerIdx
    public void PlayerDeath(Entity player_Killed, Entity player_Killer) {
        //WIP-----------------------------------------------------------------
        //Sends a Message to the players that a player has died based on their names.
        // also updates the scoreboard based on their player stats using the player[] arrays and playerinfo 
    }
    #endregion

}
