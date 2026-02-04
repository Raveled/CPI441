using UnityEngine;
using System.Collections.Generic;

public class Entity : MonoBehaviour
{
    public enum Team : int { NULL = 0, TEAM1 = 1, TEAM2 = 2, NEUTRAL = 3 };
    //Required Setup when script is added to an object
    [Header("Entity Setup")]
    [SerializeField] protected string entityName = "";
    [SerializeField] protected SO_EntityStatBlock statBlock = null;
    [SerializeField] protected Team team = Team.NULL;
    [SerializeField] protected bool canMove = true;
    [SerializeField] protected bool canDefaultAttack = true;
    [Tooltip("Only Core & Guardian Tower need this")]
    [SerializeField] Entity protector;
    [Space]
    [SerializeField] protected int reward_Gold = 0;
    [SerializeField] protected int reward_XP = 0;
    [SerializeField] float rewardRange = 50f;
    [Space]
    //Stats that are visible in editor
    [Header("Entity Debug")]
    [Tooltip("Yellow Circle")]
    [SerializeField] bool showRewardRange = false;
    [Space]
    [SerializeField] protected int maximumHitPoints = 0;
    [SerializeField] protected int currentHitPoints = 0;
    [SerializeField] protected float moveSpeed = 0;
    [SerializeField] protected int attackPower = 0;
    [SerializeField] protected float defaultAttackCooldown = 0;
    [SerializeField] protected bool isDead = false;
    
    //Setup
    [SerializeField] protected List<Team> enemyTeams = null;

    //In subclasses, MUST use "base.Start()" line to call this
    protected virtual void Start() {
        Setup();
    }
    //Assigns stats to GameObject based on the SO_EntityStatBlock
    private void Setup() {
        //Init
        statBlock = Object.Instantiate(statBlock);
        maximumHitPoints = statBlock.BaseHitPoints;
        currentHitPoints = maximumHitPoints;
        moveSpeed = statBlock.BaseMoveSpeed;
        attackPower = statBlock.BaseAttackPower;
        defaultAttackCooldown = statBlock.BaseDefaultAttackCooldown;

        //Enemy Team Setup
        if (team == Entity.Team.TEAM1) {
            enemyTeams.Add(Entity.Team.TEAM2);
            enemyTeams.Add(Entity.Team.NEUTRAL);
        } else if (team == Entity.Team.TEAM2) {
            enemyTeams.Add(Entity.Team.TEAM1);
            enemyTeams.Add(Entity.Team.NEUTRAL);
        } else if (team == Entity.Team.NEUTRAL) {
            enemyTeams.Add(Entity.Team.TEAM1);
            enemyTeams.Add(Entity.Team.TEAM2);
        }
    }
    //Basic logic for entity taking damage. Returns true on death, false on no death
    public virtual bool TakeDamage(int damage, Entity damageOrigin) {
        //Lower health by damage amount. If it is 0 or below, it is destroyed
        if (!protector || protector.GetIsDead()) {
            currentHitPoints -= damage;
            if (currentHitPoints <= 0) {
                Die(damageOrigin);
                return true;
            }
        }
        return false;
    }
    public virtual void Heal(int healAmount)
    {
        currentHitPoints += healAmount;
        if (currentHitPoints > maximumHitPoints) currentHitPoints = maximumHitPoints;
    }
    public virtual void GainMaxHealth(int maxHealthAmount)
    {
        maximumHitPoints += maxHealthAmount;
        currentHitPoints += maxHealthAmount;
    }
    //Basic logic for dying/destroying self
    protected virtual void Die(Entity damageOrigin) {
        isDead = true;
        DistributeReward();
    }

    //Give the gold+xp reward to the closest players in range. Only called on death
    protected virtual void DistributeReward() {
        //Get All Possible Players
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        List<Player> enemyPlayersInRange = new List<Player>();

        //Get the enemy team players that are in reward range
        foreach (Player p in players) {
            if (GetEnemyTeams().Contains(p.GetTeam())) {
                float distance = Vector3.Distance(p.gameObject.transform.position, transform.position);
                if(distance < rewardRange) {
                    enemyPlayersInRange.Add(p);
                }
            }
        }

        //If 3+ players in range, split the reward amount
        if(enemyPlayersInRange.Count > 2) {
            reward_Gold = (int)Mathf.Floor((float)reward_Gold / 3);
            reward_XP = (int)Mathf.Floor((float)reward_XP / 3);
        }
        //give the reward amount to each player in range
        foreach (Player p in enemyPlayersInRange) {
            p.IncreaseGoldTotal(reward_Gold);
            p.IncreaseXPTotal(reward_XP);
        }
    }
    protected virtual void OnDrawGizmos() {
        if (showRewardRange) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rewardRange);
        }
    }

    #region Setters
    public void SetTeam(Team t) {
        team = t;
    }
    #endregion
    #region Getters
    public Team GetTeam() {
        return team;
    }
    public List<Team> GetEnemyTeams() {
        return enemyTeams;
    }
    public string GetName() {
        return name;
    }
    public bool GetIsDead() {
        return isDead;
    }
    #endregion
}
