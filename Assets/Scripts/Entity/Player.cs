using UnityEngine;

public class Player : Entity
{
    [Header("Player Debug")]
    [SerializeField] SO_PlayerInfo playerStats;
    protected override void Start() {
        base.Start();
        playerStats = new SO_PlayerInfo();
    }
    protected override void Die(Entity damageOrigin) {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        playerStats.DeathCount = playerStats.DeathCount + 1;
        if(damageOrigin is Player) {
            Player p = (Player)damageOrigin;
            p.KilledPlayer();
        }
        gameManager.PlayerDeath(this, damageOrigin);
        base.Die(damageOrigin);
    }
    public void KilledPlayer() {
        playerStats.KillCount = playerStats.KillCount + 1;
    }
}
