using UnityEngine;

public class Entity : MonoBehaviour
{
    //Stats that are visible in editor
    [Header("DEBUG - DON'T SET IN EDITOR")]
    [SerializeField] protected int maximumHitPoints = 0;
    [SerializeField] protected int currentHitPoints = 0;
    [SerializeField] protected float moveSpeed = 0;
    [SerializeField] protected int attackPower = 0;
    [SerializeField] protected float defaultAttackCooldown = 0;
    [Space]
    //Required Setup when script is added to an object
    [Header("Setup")]
    [SerializeField] SO_EntityStatBlock statBlock = null;
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
        maximumHitPoints = statBlock.BaseHitPoints;
        currentHitPoints = maximumHitPoints;
        moveSpeed = statBlock.BaseMoveSpeed;
        attackPower = statBlock.BaseAttackPower;
        defaultAttackCooldown = statBlock.BaseDefaultAttackCooldown;
    }
    //Basic logic for entity taking damage
    protected virtual void TakeDamage(int damage) {
        currentHitPoints -= damage;
        if(currentHitPoints <= 0) {
            DestroyThis();
        }
    }
    //Basic logic for dying/destroying self
    protected virtual void DestroyThis() {
        Destroy(gameObject);
    }
    //Getter for Team value
    public Team GetTeam() {
        return team;
    }

}
