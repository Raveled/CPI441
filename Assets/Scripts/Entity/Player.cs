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

public class Player : Entity
{
    [Header("Player Debug")]
    [SerializeField] SyncVar<int> playerLevel = new(1);
    [SerializeField] SyncVar<int> goldTotal = new(0);
    [SerializeField] SyncVar<int> xpTotal = new(0);
    [SerializeField] MinimapTracker minimapTracker = null;
    SO_PlayerInfo playerInfoSO = null;
    List<Tower> friendlyTowers;

    public PredictedPlayerMovement predictedMovement = null;
    public PlayerID playerID;
    public SyncVar<string> character = new("");

    private GameObject parentObject;

    protected override void OnSpawned(bool asServer)
    {
        StartCoroutine(DelayedSpawn(asServer));
    }

    private IEnumerator DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(0.05f);

        base.OnSpawned(asServer);

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

        // Find PlayerID
        playerID = GetPlayerID();

        if (isServer)
        {
            Debug.Log("[PLAYER] OnSpawned Called on SERVER for Player ID: " + playerID + " | IsLocalPlayer: " + isLocalPlayer());

            // Cross Reference PlayerInfo with GameManager Instance
            // GameManager playerInfo list will be a server side authority of player features like Team/Character
            GameManager.PlayerInfo? playerInfo = GameManager.Instance.GetPlayerConfiguration(playerID);
            if (playerInfo != null)
            {
                GameManager.PlayerInfo playerInfoNN = (GameManager.PlayerInfo) playerInfo;
                team.value = (Entity.Team) playerInfoNN.team;
                character.value = playerInfoNN.character;

                //Debug.Log("[PLAYER]  Player ID: " + playerID + " | Team: " + team.value);
                //GameManager.Instance.DebugPrintPlayersInfo();

                // Tell all clients to do their LOCAL-only setup
                RPC_InitializePlayerLocals();
            }
            else Debug.Log("[PLAYER - WARNING] NO PLAYER INFO FOUND");
        }

        if (isLocalPlayer() && minimapTracker != null)
        {
            minimapTracker.AttachMinimapCamera();
        }
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

        //Debug.Log($"[Client] Player {GetPlayerID()} locals initialized, team: {GetTeam()}");
    }

    private void TrySpawnNetworkIdentity()
    {
        if (NetworkManager.main == null)
        {
            Debug.LogError("NetworkManager.main is null!");
            return;
        }

        if (!NetworkManager.main.isServer) return;

        if (isSpawned)
        {
            Debug.Log("Player already spawned");
            return;
        }

        if (predictedMovement != null && predictedMovement.owner.HasValue)
        {
            //Debug.Log($"Spawning NetworkIdentity for player {predictedMovement.owner.Value}");
            NetworkManager.main.Spawn(this.gameObject);
        }
        else
        {
            Debug.LogError("Cannot spawn - predictedMovement or owner is null!");
        }
    }

    public override bool TakeDamage(int damage, Entity damageOrigin) {
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
    }

    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);
        Debug.Log("Player: " + entityName + " has died");

        //Update PlayerStats
        playerInfoSO.DeathCount = playerInfoSO.DeathCount + 1;
        if(damageOrigin is Player p) {
            p.KilledPlayer();
        }

        //Send GameManager message of event
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        gameManager.PlayerDeath(this, damageOrigin);

        //WIP--------------------------------------------------------
        //go into death mode - body nonexistant, move on respawn
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
        return predictedMovement.predictionManager.localPlayer == GetPlayerID();
    }
}
