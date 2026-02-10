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
    //Stats that are visible in editor
    [Header("Entity Debug")]
    [SerializeField] protected int goldReward = 0;
    [SerializeField] protected int maximumHitPoints = 0;
    [SerializeField] protected int currentHitPoints = 0;
    [SerializeField] protected float moveSpeed = 0;
    [SerializeField] public int attackPower = 0;
    [SerializeField] protected float defaultAttackCooldown = 0;
    [Space]
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
        goldReward = statBlock.GoldReward;
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
    //Basic logic for entity taking damage
    public virtual void TakeDamage(int damage, Entity damageOrigin) {
        //Lower health by damage amount. If it is 0 or below, it is destroyed
        if (!protector || protector.GetIsDead()) {
            currentHitPoints -= damage;
            if (currentHitPoints <= 0) {
                DestroyThis(damageOrigin);
            }
        }
    }
    //Basic logic for dying/destroying self
    protected virtual void DestroyThis(Entity damageOrigin) {
        Destroy(gameObject);
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
