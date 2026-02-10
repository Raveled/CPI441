using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Assertions.Must;
using PurrNet;
using NUnit.Framework;

public class NonPlayerEntity : Entity
{
    //Setup fields
    [Header("NPE Setup")]

    [Space]
    [SerializeField] protected Transform attackRangeOrigin = null;
    [SerializeField] protected NPEDetectLogic npeDetectLogic = null;
    [SerializeField] protected UnityEngine.UI.Slider healthBar = null;
    [Space]
    [SerializeField] bool canTargetTower = false;
    [SerializeField] bool canTargetCore = false;
    [SerializeField] bool canTargetMinion = false;
    [SerializeField] bool canTargetPlayer = false;
    [Space]
    //Debug fields
    [Header("NPE Debug")]
    [Tooltip("Red Circle")]
    [SerializeField] bool showAttackRange = true;
    [Tooltip("Blue Line")]
    [SerializeField] bool showForwardDirection = true;
    [Space]

    // NETWORKED
    [SerializeField] protected SyncVar<NetworkID?> targetId = new(null);
    [SerializeField] protected SyncVar<bool> hasTarget = new(false);
    [SerializeField] protected SyncVar<float> attackCooldownTimer = new(0f);
    [SerializeField] protected SyncVar<float> attackRange = new(10f);

    // LOCAL (non-networked) state
    [SerializeField] bool canSearchForTarget = true;
    [SerializeField] bool canAttackTimer = true;
    [SerializeField] List<Entity> entitiesInRange; //for debug purposes, used in FindTarget()


    //Targeting fields SERVER ONLY
    protected Entity closestMinion = null;
    protected float minDistanceMinion = Mathf.Infinity;
    protected Entity closestPlayer = null;
    protected float minDistancePlayer = Mathf.Infinity;
    protected Entity closestTower = null;
    protected float minDistanceTower = Mathf.Infinity;
    protected Entity closestCore = null;
    protected float minDistanceCore = Mathf.Infinity;

    protected override void Start() {
        base.Start();
        entitiesInRange = new List<Entity>();
        npeDetectLogic.SetNPE(this);

        UpdateHealthBar();
    }

    protected override void OnHealthChanged(int newHealth)
    {
        base.OnHealthChanged(newHealth);
        UpdateHealthBar();
    }

    protected override void OnDeathClient(NetworkID? damageOriginId)
    {
        base.OnDeathClient(damageOriginId);
    }

    //Update healthBar UI Element
    void UpdateHealthBar()
    {
        if (healthBar == null) return;

        healthBar.maxValue = maximumHitPoints.value;
        healthBar.value = currentHitPoints.value;
    }
    protected override void Die(Entity damageOrigin) {
        base.Die(damageOrigin);

        //Destroy self
        Destroy(gameObject);
    }

    //Move
    protected virtual void Move() {
        if (!canMove) return;
    }
    protected virtual void Move(Entity currentTarget) {
        if (!canMove) return;
    }

    //Attack
    protected virtual void Attack() {
        if (!canDefaultAttack) return;
    }
    protected virtual void Attack(Entity currentTarget) {
        if (!canDefaultAttack) return;
    }

    //Cooldown for attacks
    protected void AttackTimer() {
        if (!canAttackTimer) return;
        if (!isServer) return; // Only execute timer logic on the server

        if (attackCooldownTimer >= 0) {
            attackCooldownTimer.value -= Time.deltaTime;
        }
    }
    //Gets the closest entity in detect range and sets it as target
    protected virtual void FindTarget() {
        if (!canSearchForTarget || !isServer) return ; // Only execute targeting logic on the server

        Entity current = GetTarget();
        if (current == null || current.GetIsDead()) {
            //Reset Data
            ResetTarget();

            //Get List of entities in range
            entitiesInRange = npeDetectLogic.GetEnemiesInRange();

            //Loop through each entity in range
            foreach (Entity e in entitiesInRange) {
                //Get distance from this to entity
                if (!e || e.GetIsDead()) continue;

                if (e == this) continue;

                if (e.GetTeam() == Team.NULL || e.GetTeam() == GetTeam()) continue; // Don't target entities on the same team

                float dist = Vector3.Distance(transform.position, e.gameObject.transform.position);

                //Set closest entity type
                if (e is Minion && dist < minDistanceMinion) {
                    closestMinion = e;
                    minDistanceMinion = dist;
                } else if (e is Player && dist < minDistancePlayer) {
                    closestPlayer = e;
                    minDistancePlayer = dist;
                } else if (e is Tower && dist < minDistanceTower) {
                    closestTower = e;
                    minDistanceTower = dist;
                } else if (e is Core && dist < minDistanceCore) {
                    closestCore = e;
                    minDistanceCore = dist;
                }
            }

            //In order of Tower > Core > Minion > Player, set target equal to the closest
            Entity newTarget = null;
            if (canTargetTower && closestTower) newTarget = closestTower;
            else if (canTargetCore && closestCore) newTarget = closestCore;
            else if (canTargetMinion && closestMinion) newTarget = closestMinion;
            else if (canTargetPlayer && closestPlayer) newTarget = closestPlayer;
            else newTarget = null;


            //DONT use GetNetworkID(entity).Value


            if (newTarget != null && newTarget.GetTeam() == GetTeam()) {
                Debug.LogError("Attempting to target entity on same team. This should never happen. Check targeting logic.");
                newTarget = null;
            }

            SetTarget(newTarget);
        }

    }

    // SERVER
    protected void SetTarget(Entity newTarget)
    {
        if (!isServer) return;



        if (newTarget == null)
        {
            targetId.value = null;
            hasTarget.value = false;
        }
        else
        {
            targetId.value = newTarget.GetNetworkID(isServer);
            hasTarget.value = true;


        }
    }

    //Resets the target, called from NPEDetectLogic
    public void ResetTarget() {
        if (!isServer) return;

        closestMinion = null;
        minDistanceMinion = Mathf.Infinity;
        closestPlayer = null;
        minDistancePlayer = Mathf.Infinity;
        closestTower = null;
        minDistanceTower = Mathf.Infinity;
        closestCore = null;
        minDistanceCore = Mathf.Infinity;

        SetTarget(null);
    }
    //Visualization for ranges using Gizmos
    protected override void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (showAttackRange) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackRangeOrigin.position, attackRange);
        }
        if (showForwardDirection) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
        }
    }
    //For pausing the game
    public void Freeze(bool freezeNPE) {
        canMove = !freezeNPE;
        canDefaultAttack = !freezeNPE;
        canAttackTimer = !freezeNPE;
        canSearchForTarget = !freezeNPE;
    }
    #region Getters
    public Entity GetTarget()
    {
        if (!targetId.value.HasValue) return null;

        return GetEntityByNetworkID(targetId.value);
    }

    public NetworkID? GetTargetId()
    {
        return targetId.value;
    }

    public bool HasTarget()
    {
        return hasTarget.value;
    }
    #endregion
}
