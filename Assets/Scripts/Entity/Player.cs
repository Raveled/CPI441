using UnityEngine;

public class Player : Entity
{
    [Header("Player Debug")]
    [SerializeField] SO_PlayerInfo playerInfo = null;
    [SerializeField] int playerLevel = 1;
    [SerializeField] int goldTotal = 0;
    [SerializeField] int xpTotal = 0;
    protected override void Start() {
        base.Start();
        playerInfo = new SO_PlayerInfo();
    }
    public override bool TakeDamage(int damage, Entity damageOrigin) {
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
    }
    //Getts
    public SO_PlayerInfo GetPlayerInfo() {
        return playerInfo;
    }

}
