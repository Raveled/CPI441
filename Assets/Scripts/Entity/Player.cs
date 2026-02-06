using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Player : Entity
{
    [Header("Player Debug")]
    [SerializeField] int playerLevel = 1;
    [SerializeField] int goldTotal = 0;
    [SerializeField] int xpTotal = 0;
    SO_PlayerInfo playerInfo = null;
    List<Tower> friendlyTowers;
    protected override void Start() {
        base.Start();
        playerInfo = ScriptableObject.CreateInstance<SO_PlayerInfo>();

        //Fill towers with same team towers
        friendlyTowers = new List<Tower>();
        Tower[] allTowers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
        foreach(Tower t in allTowers)
        {
            if (GetTeam() == t.GetTeam()) friendlyTowers.Add(t);
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
    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);
        Debug.Log("Player: " + entityName + " has died");

        //Update PlayerStats
        playerInfo.DeathCount = playerInfo.DeathCount + 1;
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
        playerInfo.KillCount = playerInfo.KillCount + 1;
    }
    //Called by entity dying, increase gold amount;
    public void IncreaseGoldTotal(int addAmount) {
        goldTotal += addAmount;
    }
    //Called by entity dying, increase xp amount;
    public void IncreaseXPTotal(int addAmount) {
        xpTotal += addAmount;
        CheckLevelUp();
    }
    //Check for player level up
    void CheckLevelUp() {
        //WIP-----------------------------------------------------------
        if (xpTotal == 100) playerLevel++;
    }
    //Getts
    public SO_PlayerInfo GetPlayerInfo() {
        return playerInfo;
    }

}
