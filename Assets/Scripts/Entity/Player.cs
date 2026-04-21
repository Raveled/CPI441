using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using PurrNet;
using System.Data.Common;
using PurrNet.Modules;
using System.Collections;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms;

public class Player : Entity
{
    [Header("Player Settings/Debug")]
    [SerializeField] SyncVar<int> playerLevel = new(1);
    [SerializeField] SyncVar<int> goldTotal = new(0);
    [SerializeField] SyncVar<int> xpTotal = new(0);
    [SerializeField] MinimapTracker minimapTracker = null;
    [SerializeField] protected UnityEngine.UI.Slider healthBar = null;
    SO_PlayerInfo playerInfoSO = null;
    List<Tower> friendlyTowers;

    public PredictedPlayerMovement predictedMovement = null;

    public PlayerID playerID;
    public SyncVar<string> character = new("");

    private GameObject parentObject;
    private UnityEngine.UI.Slider healthBarSliderUI;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnTime = 10f;
    [SerializeField] private Vector3 outOfBoundsPosition = new Vector3(0f, -1000f, 0f);

    [SerializeField] RespawnUIController respawnUI;

    protected override void OnSpawned(bool asServer)
    {
        StartCoroutine(DelayedSpawn(asServer));
    }

    protected void Update()
    {
        if (!isServer) UpdateHealthBars();
    }

    private IEnumerator DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(0.05f);

        base.OnSpawned(asServer);

        // Fix sizing and placement issue
        GameObject thisPlayerObject = this.gameObject;
        if (thisPlayerObject.name.Contains("PlayerRoot"))
        {
            thisPlayerObject.transform.localScale = Vector3.one;
            thisPlayerObject.transform.localPosition = Vector3.zero;
            thisPlayerObject.transform.localRotation = Quaternion.identity;
        }

        if (!isServer)
        {
            PredictedPlayerMovement[] ppMovements = FindObjectsByType<PredictedPlayerMovement>(FindObjectsSortMode.None);
            for (int i = 0; i < ppMovements.Count(); i++)
            {
                //Debug.Log($"Checking PP OWNER: {ppMovements[i].owner} [{i}/{ppMovements.Count()}] for player owner: {owner}");
                if (ppMovements[i].owner == owner)
                {
                    //Debug.Log($"PP OWNER: {ppMovements[i].owner} found for player owner: {owner}");
                    predictedMovement = ppMovements[i];
                    predictedMovement._player = this;
                    transform.SetParent(predictedMovement.transform);
                    transform.position = transform.parent.transform.position;
                }
            }
        }

        if (predictedMovement != null) this.transform.SetParent(predictedMovement.transform);
        else
        {
            parentObject = transform.parent.gameObject;
            if (predictedMovement == null) predictedMovement = parentObject.GetComponent<PredictedPlayerMovement>();
        }

        predictedMovement.LoadStatsFromPlayer();

        // Find PlayerID
        playerID = GetPlayerID();

        if (isServer)
        {
            // Cross Reference PlayerInfo with GameManager Instance
            // GameManager playerInfo list will be a server side authority of player features like Team/Character
            GameManager.PlayerInfo? playerInfo = GameManager.Instance.GetPlayerConfiguration(playerID);
            if (playerInfo != null)
            {
                GameManager.PlayerInfo playerInfoNN = (GameManager.PlayerInfo) playerInfo;
                SetTeam((Entity.Team) playerInfoNN.team);
                character.value = playerInfoNN.character;

                Debug.Log("[PLAYER] OnSpawned Called on SERVER for Player ID: " + playerID + " | IsLocalPlayer: " + isLocalPlayer() + " | Team: " + team.value + " | Character: " + character.value);
                //GameManager.Instance.DebugPrintPlayersInfo();

                // Tell all clients to do their LOCAL-only setup
                RPC_InitializePlayerLocals();

                this.isDead.value = false;
            }
            else Debug.Log("[PLAYER - WARNING] NO PLAYER INFO FOUND");
        }

        if (isLocalPlayer())
        {
            if (minimapTracker != null) minimapTracker.AttachMinimapCamera();
            InitHealthBars();
            InitRespawnUI();
        }
    }

    private void InitRespawnUI()
    {
        respawnUI = GameObject.Find("RespawnUI").GetComponent<RespawnUIController>();
        respawnUI.Hide();
    }

    private void InitHealthBars()
    {
        if (healthBar != null) healthBar.transform.parent.gameObject.SetActive(false);
        healthBarSliderUI = GameObject.Find("HealthSlider").GetComponent<UnityEngine.UI.Slider>();
        UpdateHealthBars();
    }

    [ObserversRpc(bufferLast: true)]
    private void RPC_InitializePlayerLocals()
    {
        // ScriptableObject is local-only, fine here
        playerInfoSO = ScriptableObject.CreateInstance<SO_PlayerInfo>();

        // team.value is already synced by the SyncVar — safe to read here
        friendlyTowers = new List<Tower>();
        Tower[] allTowers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
        foreach (Tower t in allTowers)
        {
            if (GetTeam() == t.GetTeam()) friendlyTowers.Add(t);
        }

        Debug.Log($"[Client] Player {GetPlayerID()} locals initialized, team: {GetTeam()}");
    }

    public override bool TakeDamage(int damage, Entity damageOrigin) {
        if (isDead.value) return false;

        if (friendlyTowers == null) 
        {
            Debug.Log($"[Player] {playerID} has no friendly towers list! This should have been initialized in RPC_InitializePlayerLocals.");
            
            return base.TakeDamage(0, damageOrigin);
        }

        //Check Friendly Tower Aggro
        Tower closestTower = null;
        float minDist = Mathf.Infinity;

        //Loop through all friendly towers
        for(int i = friendlyTowers.Count - 1; i >= 0; i--)
        {
            if (friendlyTowers[i])
            {
                //Get closest friendly tower
                float dist = Vector3.Distance(friendlyTowers[i].transform.position, transform.position);

                //If closer, set as closest
                if(dist < minDist)
                {
                    closestTower = friendlyTowers[i];
                    minDist = dist;
                }
            } else
            {
                friendlyTowers.RemoveAt(i);
            }
        }
        if (closestTower)
        {
            closestTower.OverrideTarget(damageOrigin);
        }

        return base.TakeDamage(damage, damageOrigin);
    }

    protected override void OnHealthChanged(int newHealth)
    {
        base.OnHealthChanged(newHealth);
        UpdateHealthBars();
    }
    public int GetGoldTotal()
    {
        return goldTotal.value;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        if (goldTotal.value < amount)
        {
            return false;
        }

        goldTotal.value -= amount;
        return true;
    }
    //Update healthBar UI Element
    void UpdateHealthBars()
    {
        if (healthBar != null)
        {
            healthBar.maxValue = maximumHitPoints.value;
            healthBar.value = currentHitPoints.value;
        }

        if (healthBarSliderUI != null)
        {
            healthBarSliderUI.maxValue = maximumHitPoints.value;
            healthBarSliderUI.value = currentHitPoints.value;
        }
    }

    protected override void Die(Entity damageOrigin) {
        if (playerInfoSO == null) return; // Should never happen, but just in case

        base.Die(damageOrigin);
        currentHitPoints.value = 0;
        UpdateHealthBars();
        Debug.Log("Player: " + GetPlayerID() + " has died");

        if (isLocalPlayer() && respawnUI != null) respawnUI.Show();

        // Update PlayerStats
        playerInfoSO.DeathCount = playerInfoSO.DeathCount + 1;
        if(damageOrigin is Player p) {
            p.KilledPlayer();
        }

        // Send GameManager message of event
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.PlayerDeath(this, damageOrigin);

        // Respawn Routine
        if (isServer) StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // Move character out of the world immediately, notify all clients
        RPC_MoveToOutOfBounds();

        yield return new WaitForSeconds(respawnTime);

        // Find the correct spawn point for this player's team
        Vector3 spawnPosition = GetTeamSpawnPoint();

        // Reset health server-side
        currentHitPoints.value = maximumHitPoints.value;
        UpdateHealthBars();

        isDead.value = false;

        // Tell all clients to teleport and refresh UI
        RPC_Respawn(spawnPosition);
    }

    private Vector3 GetTeamSpawnPoint()
    {
        // Spawn points should be named/tagged starting with "T1" or "T2"
        string prefix = GetTeam() == Entity.Team.TEAM1 ? "T1" : "T2";

        // Gather all matching spawn points
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> spawnPoints = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                spawnPoints.Add(obj);
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[Player] No spawn points found for prefix '{prefix}'. Respawning at origin.");
            return Vector3.zero;
        }

        // Pick a random one so players don't all stack on the same point
        return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)].transform.position;
    }

    [ObserversRpc]
    private void RPC_MoveToOutOfBounds()
    {
        if (predictedMovement != null)
        {
            predictedMovement.transform.position = outOfBoundsPosition;
            predictedMovement._rigidbody.linearVelocity = Vector3.zero;
        }
    }

    [ObserversRpc]
    private void RPC_Respawn(Vector3 spawnPosition)
    {
        if (predictedMovement != null)
        {
            predictedMovement.transform.position = spawnPosition;
            predictedMovement._rigidbody.linearVelocity = Vector3.zero;
        }

        if (predictedMovement.transform.position == outOfBoundsPosition)
        {
            Debug.LogWarning($"[Player] {entityName} was still at out-of-bounds position during RPC_Respawn. Teleporting to spawn point.");
            predictedMovement.transform.position = spawnPosition;
        }

        Debug.Log($"[Player] {entityName} respawned at {spawnPosition}");
        if (isLocalPlayer() && respawnUI != null) respawnUI.Hide();
    }

    //Update Player stats on kill
    public void KilledPlayer() {
        playerInfoSO.KillCount = playerInfoSO.KillCount + 1;
    }
    //Called by entity dying, increase gold amount;
    public void IncreaseGoldTotal(int addAmount) {
        goldTotal.value += addAmount;
    }
    //Called by entity dying, increase xp amount;
    public void IncreaseXPTotal(int addAmount) {
        xpTotal.value += addAmount;
        CheckLevelUp();
    }
    //Check for player level up
    void CheckLevelUp() {
        //WIP-----------------------------------------------------------
        if (xpTotal == 100) playerLevel.value++;
    }
    //Getts
    public SO_PlayerInfo GetPlayerInfoSO() {
        return playerInfoSO;
    }

    // Helper
    public PlayerID GetPlayerID()
    {
        foreach (var player in networkManager.players) 
        {
            if (player == predictedMovement.owner.Value) 
            {
                playerID = player;
                return playerID;
            }
        }
        
        return playerID;
    }

    public bool isLocalPlayer()
    {
        if (predictedMovement == null) return false;

        return predictedMovement.predictionManager.localPlayer == GetPlayerID();
    }

    [ObserversRpc]
    public void RPC_ShowGameResult(Entity.Team? result)
    {
        if (!isLocalPlayer() || result == null) return;

        bool iWon = false;
        if (GetTeam() == result) iWon = true;

        UIManager.Instance.ShowEndScreen(iWon);
    }
}
