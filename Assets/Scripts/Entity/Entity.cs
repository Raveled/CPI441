using PurrNet;
using UnityEngine;
using System.Collections.Generic;
using PurrNet.Prediction;
using PurrNet.Modules;
using System.Collections;

public class Entity : NetworkBehaviour
{
    public enum Team : int { NULL = 0, TEAM1 = 1, TEAM2 = 2, NEUTRAL = 3 };
    //Required Setup when script is added to an object
    [Header("Entity Setup")]
    [SerializeField] protected string entityName = "";
    [SerializeField] protected SO_EntityStatBlock statBlock = null;
    [SerializeField] protected SyncVar<Team> team = new(Team.NULL);
    [SerializeField] protected bool canMove = true;
    [SerializeField] protected bool canDefaultAttack = true;

    [Tooltip("Only Core & Guardian Tower need this set in inspector")]
    [SerializeField] PredictedObjectID[] protector = null; // Changed from Entity[] to PredictedObjectID[] for better network referencing
    [SerializeField] float rewardRange = 50f;
    [Space]

    //Stats that are visible in editor
    [Header("Entity Debug")]
    /*[SerializeField] protected int goldReward = 0;
    [SerializeField] public int maximumHitPoints = 0;
    [SerializeField] public int currentHitPoints = 0;                   //Modified by Theo - made this SyncVar and moved it to networked stats section, since health needs to be synchronized across clients for damage - removed safe mode
    [SerializeField] public float moveSpeed = 0;
    [SerializeField] public int attackPower = 0;
    [SerializeField] protected float defaultAttackCooldown = 0;*/
    [Space]

    // NETWORKED STATS
    [SerializeField] public SyncVar<int> currentHitPoints = new(0);
    [SerializeField] public SyncVar<int> maximumHitPoints = new(0);
    [SerializeField] protected SyncVar<float> moveSpeed = new(0f);
    [SerializeField] protected SyncVar<float> acceleration = new(0f);
    [SerializeField] protected SyncVar<float> planarDamping = new(0f);
    [SerializeField] protected SyncVar<float> jumpForce = new(0f);
    [SerializeField] protected SyncVar<float> jumpCooldown = new(0f);
    [SerializeField] public SyncVar<int> attackPower = new(0);
    [SerializeField] public SyncVar<float> defaultAttackCooldown = new(0f);
    [SerializeField] public SyncVar<int> reward_Gold = new(0);
    [SerializeField] public SyncVar<int> reward_XP = new(0);
    [SerializeField] public SyncVar<bool> isDead = new(false);

    //Setup
    [SerializeField] protected List<Team> enemyTeams = null;

    //In subclasses, MUST use "base.Start()" line to call this
    protected virtual void Start()
    {
        Setup();
    }

    //Assigns stats to GameObject based on the SO_EntityStatBlock
    private void Setup()
    {
        //Enemy Team Setup
        enemyTeams.Clear();
        if (team.value == Entity.Team.TEAM1)
        {
            enemyTeams.Add(Entity.Team.TEAM2);
            enemyTeams.Add(Entity.Team.NEUTRAL);
        }
        else if (team.value == Entity.Team.TEAM2)
        {
            enemyTeams.Add(Entity.Team.TEAM1);
            enemyTeams.Add(Entity.Team.NEUTRAL);
        }
        else if (team.value == Entity.Team.NEUTRAL)
        {
            enemyTeams.Add(Entity.Team.TEAM1);
            enemyTeams.Add(Entity.Team.TEAM2);
        }

        //In case not called from SetStats from spawning in
        StartCoroutine(SetupStats());
    }

    public IEnumerator SetupStats()
    {
        yield return new WaitUntil(() => isSpawned);

        if (!isServer)
        {
            SetupStatsServerRpc();
        }
        else
        {
            ApplySetupStats();
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void SetupStatsServerRpc()
    {
        ApplySetupStats();
    }

    // SERVER ONLY - Apply stat setup
    [ServerRpc]
    private void ApplySetupStats()
    {
        if (!isServer)
            return;

        Debug.Log($"Applying stats for {entityName} on team {team.value}");

        maximumHitPoints.value = statBlock.BaseHitPoints;
        currentHitPoints.value = maximumHitPoints.value;
        moveSpeed.value = statBlock.BaseMoveSpeed;
        acceleration.value = statBlock.BaseAcceleration;
        planarDamping.value = statBlock.BasePlanarDamping;
        jumpForce.value = statBlock.BaseJumpForce;
        jumpCooldown.value = statBlock.BaseJumpCooldown;
        attackPower.value = statBlock.BaseAttackPower;
        defaultAttackCooldown.value = statBlock.BaseDefaultAttackCooldown;
        reward_Gold.value = statBlock.RewardGold;
        reward_XP.value = statBlock.RewardXP;
    }

    //Basic logic for entity taking damage. Returns true on death, false on no death
    public virtual bool TakeDamage(int damage, Entity damageOrigin)
    {
        if (!isServer)
        {
            Debug.Log($"[TakeDamage] {gameObject.name} with ID: {damageOrigin.GetNetworkID(isServer)} is requesting to take {damage} damage from {damageOrigin?.name}");
            // Client requests server to apply damage
            if (damageOrigin != null) RequestDamageServerRpc(damage, damageOrigin.GetNetworkID(isServer));
            else RequestDamageServerRpc(damage, null);
            return isDead.value; // Return current state for client
        }

        Debug.Log($"[TakeDamage] Server is applying {damage} damage from {damageOrigin?.name} to {name}");
        // Server applies damage directly
        if (damageOrigin != null) ApplyDamageServer(damage, damageOrigin.GetNetworkID(isServer));
        else ApplyDamageServer(damage, null);
        return isDead.value;
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestDamageServerRpc(int damage, NetworkID? originId)
    {
        Debug.Log($"[TakeDamageServerRpc] Received damage request: {damage} from origin ID: {originId}");
        ApplyDamageServer(damage, originId);
    }

    [ServerRpc]
    private void ApplyDamageServer(int damage, NetworkID? originId)
    {
        if (isDead.value || !isServer)
            return;

        Debug.Log($"[TakeDamage] {name} is taking {damage} damage from {originId}");

        // Check if protector is alive
        bool protectorAlive = false;
        if (protector != null)
        {
            foreach (var protectorId in protector)
            {
                if (protectorId.Equals(null))
                    continue;

                var protector = FindFirstObjectByType<PredictionManager>().hierarchy.GetComponent<Entity>(protectorId);
                if (protector != null && !protector.isDead.value)
                {
                    protectorAlive = true;
                    break;
                }
            }
        }

        //Debug.Log($"[TakeDamage] Protector alive: {protectorAlive}");

        // Deal damage if no protector alive
        if (!protectorAlive)
        {
            //Debug.Log($"[TakeDamage] {name} is taking damage because no protector is alive.");
            currentHitPoints.value -= damage;

            if (currentHitPoints.value <= 0)
            {
                currentHitPoints.value = 0;
                isDead.value = true;
                OnDeath(originId);
            }
        }

        // Notify all clients of health update
        if(!GetIsDead())NotifyHealthChanged(currentHitPoints.value);

        return;
    }

    [ObserversRpc]
    private void NotifyHealthChanged(int newHealth)
    {
        // This method can be used to trigger client-side effects when health changes, if needed
        Debug.Log($"{entityName} health: {newHealth}/{maximumHitPoints.value}");
        OnHealthChanged(newHealth);
    }

    protected virtual void OnHealthChanged(int newHealth)
    {
        // Subclasses can override this for custom behavior
    }

    public virtual void Heal(int healAmount)
    {
        if (!isServer)
        {
            HealServerRpc(healAmount);
            return;
        }

        ApplyHeal(healAmount);
    }

    [ServerRpc(requireOwnership: false)]
    private void HealServerRpc(int healAmount)
    {
        ApplyHeal(healAmount);
    }

    // SERVER ONLY - Apply healing
    private void ApplyHeal(int healAmount)
    {
        if (isDead.value || !isServer)
            return;

        currentHitPoints.value += healAmount;
        if (currentHitPoints.value > maximumHitPoints.value) currentHitPoints.value = maximumHitPoints.value;

        NotifyHealthChanged(currentHitPoints.value);
    }

    public virtual void GainMaxHealth(int maxHealthAmount)
    {
        if (!isServer)
        {
            GainMaxHealthServerRpc(maxHealthAmount);
            return;
        }

        ApplyMaxHealthGain(maxHealthAmount);
    }

    [ServerRpc(requireOwnership: false)]
    private void GainMaxHealthServerRpc(int maxHealthAmount)
    {
        ApplyMaxHealthGain(maxHealthAmount);
    }

    // SERVER ONLY - Apply max health gain
    public virtual void ApplyMaxHealthGain(int maxHealthAmount)
    {
        if (isDead.value || !isServer)
            return;

        maximumHitPoints.value += maxHealthAmount;
        currentHitPoints.value += maxHealthAmount;

        NotifyHealthChanged(currentHitPoints.value);
    }

    //************************************************************************//
    //New Additions from Theo for Character Abilities

    // *** Attack Power Buff / Debuff *** //
    public void ModifyAttackPowerForSeconds(float multiplier, float duration)
    {
        if (!isServer)
        {
            ModifyAttackPowerForSecondsServerRpc(multiplier, duration);
            return;
        }
        StartCoroutine(ModifyAttackPowerCoroutine(multiplier, duration));
    }

    [ServerRpc(requireOwnership: false)]
    private void ModifyAttackPowerForSecondsServerRpc(float multiplier, float duration)
    {
        StartCoroutine(ModifyAttackPowerCoroutine(multiplier, duration));
    }

    private IEnumerator ModifyAttackPowerCoroutine(float multiplier, float duration)
    {
        int originalAttackPower = attackPower.value;
        attackPower.value = Mathf.RoundToInt(originalAttackPower * multiplier);

        yield return new WaitForSeconds(duration);

        if (!isDead.value) // only restore if still alive
            attackPower.value = originalAttackPower;
    }

    // *** Move Speed Buff / Debuff / Stun *** //
    public void ModifyMoveSpeedMultiplier(float multiplier, float duration)
    {
        if (!isServer)
        {
            ModifyMoveSpeedMultiplierServerRpc(multiplier, duration);
            return;
        }
        StartCoroutine(ModifyMoveSpeedCoroutine(multiplier, duration));
    }

    [ServerRpc(requireOwnership: false)]
    private void ModifyMoveSpeedMultiplierServerRpc(float multiplier, float duration)
    {
        StartCoroutine(ModifyMoveSpeedCoroutine(multiplier, duration));
    }

    private IEnumerator ModifyMoveSpeedCoroutine(float multiplier, float duration)
    {
        float originalMoveSpeed = moveSpeed.value;
        moveSpeed.value *= multiplier;

        yield return new WaitForSeconds(duration);

        if (!isDead.value)
            moveSpeed.value = originalMoveSpeed;
    }



    //************************************************************************//

    // Server Only Death Logic
    protected virtual void OnDeath(NetworkID? damageOriginId)
    {
        if (!isServer)
            return;

        // Find the entity that caused the death
        Entity damageOrigin = null;

        if (damageOriginId.HasValue)
        {
            damageOrigin = GetEntityByNetworkID(damageOriginId.Value);
        }

        if (!GetIsDead()) NotifyDeathObserversRpc(damageOriginId);
        Die(damageOrigin);
    }

    //Basic logic for dying/destroying self
    protected virtual void Die(Entity damageOrigin)
    {
        if (!isServer)
            return;

        isDead.value = true;
        DistributeReward();
    }

    [ObserversRpc]
    protected virtual void NotifyDeathObserversRpc(NetworkID? damageOriginId)
    {
        // Client-side death effects (animations, sounds, VFX, etc.)
        // Do NOT modify authoritative state here

        Debug.Log($"{entityName} has died.");
        OnDeathClient(damageOriginId);
    }

    protected virtual void OnDeathClient(NetworkID? damageOriginId)
    {
        // Subclasses can override this for custom client-side death behavior
    }

    //Give the gold+xp reward to the closest players in range. Only called on death
    protected virtual void DistributeReward()
    {
        if (!isServer)
            return;

        //Get All Possible Players
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        List<Player> enemyPlayersInRange = new List<Player>();

        //Get the enemy team players that are in reward range
        foreach (Player p in players)
        {
            if (GetEnemyTeams().Contains(p.GetTeam()))
            {
                float distance = Vector3.Distance(p.gameObject.transform.position, transform.position);
                if (distance < rewardRange)
                {
                    enemyPlayersInRange.Add(p);
                }
            }
        }

        int goldReward = reward_Gold.value;
        int xpReward = reward_XP.value;

        //If 3+ players in range, split the reward amount
        if (enemyPlayersInRange.Count > 2)
        {
            goldReward = (int)Mathf.Floor((float)goldReward / 3);
            xpReward = (int)Mathf.Floor((float)xpReward / 3);
        }

        //give the reward amount to each player in range
        foreach (Player p in enemyPlayersInRange)
        {
            p.IncreaseGoldTotal(goldReward);
            p.IncreaseXPTotal(xpReward);
        }
    }
    protected virtual void OnDrawGizmos()
    {
        /*if (showRewardRange) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rewardRange);
        }*/
    }

    #region Setters
    public void SetTeam(Team t)
    {
        team.value = t;
    }
    public void SetStatblock(SO_EntityStatBlock stats)
    {
        statBlock = Object.Instantiate(stats);
        StartCoroutine(SetupStats()); //Modified by Theo - will wait until isSpawned to apply stats, ensuring proper network synchronization
    }
    #endregion
    #region Getters
    public Team GetTeam()
    {
        return team.value;
    }
    public List<Team> GetEnemyTeams()
    {
        return enemyTeams;
    }
    public string GetName()
    {
        return name;
    }
    public bool GetIsDead()
    {
        return isDead.value;
    }
    public float GetMoveSpeed()
    {
        return moveSpeed.value;
    }
    public SO_EntityStatBlock GetEntityStatblock()
    {
        return statBlock;
    }
    #endregion

    // Helper method to find an Entity by its NetworkID
    protected Entity GetEntityByNetworkID(NetworkID? networkId)
    {
        Entity[] allEntities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
        foreach (var entity in allEntities)
        {
            if (entity.GetNetworkID(isServer) == networkId)
            {
                return entity;
            }
        }

        return null;
    }
}