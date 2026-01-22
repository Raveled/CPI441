using UnityEngine;

public class Entity : MonoBehaviour
{
    //Stats that are visible in editor
    [Header("DEBUG - DON'T SET IN EDITOR")]
    [SerializeField] protected int goldReward = 0;
    [SerializeField] protected int maximumHitPoints = 0;
    [SerializeField] protected int currentHitPoints = 0;
    [SerializeField] protected float moveSpeed = 0;
    [SerializeField] protected int attackPower = 0;
    [SerializeField] protected float defaultAttackCooldown = 0;
    [Space]
    //Required Setup when script is added to an object
    [Header("Setup")]
    [SerializeField] protected string entityName = "";
    [SerializeField] protected SO_EntityStatBlock statBlock = null;
    public enum Team : int { NULL = 0, Team1 = 1, Team2 = 2, Neutral = 3 };
    [SerializeField] protected Team team = Team.NULL;
    [SerializeField] protected bool canMove = true;
    [SerializeField] protected bool canDefaultAttack = true;

    //In subclasses, must use "base.Start()" line to call this
    protected virtual void Start() {
        SetupStats();
    }
    //Assigns stats to GameObject based on the SO_EntityStatBlock
    private void SetupStats() {
        statBlock = Object.Instantiate(statBlock);
        goldReward = statBlock.GoldReward;
        maximumHitPoints = statBlock.BaseHitPoints;
        currentHitPoints = maximumHitPoints;
        moveSpeed = statBlock.BaseMoveSpeed;
        attackPower = statBlock.BaseAttackPower;
        defaultAttackCooldown = statBlock.BaseDefaultAttackCooldown;
    }
    //Basic logic for entity taking damage
    protected virtual void TakeDamage(int damage, GameObject damageOrigin) {
        currentHitPoints -= damage;
        if(currentHitPoints <= 0) {
            DestroyThis(damageOrigin);
        }
    }
    //Basic logic for dying/destroying self
    protected virtual void DestroyThis(GameObject damageOrigin) {
        Destroy(gameObject);
    }
    #region Getters
    public Team GetTeam() {
        return team;
    }
    public string GetName() {
        return name;
    }
    #endregion
}
