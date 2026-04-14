using UnityEngine;

public class EnhancedPlayer : Entity
{
    [Header("Player Inventory")]
    [SerializeField] private Inventory inventory;
    
    [Header("Modified Stats")]
    [SerializeField] private int modifiedMaxHitPoints;
    [SerializeField] private int modifiedAttackPower;
    [SerializeField] private float modifiedMoveSpeed;
    [SerializeField] private float modifiedAttackSpeed;
    [SerializeField] private int defense;
    
    private StatModifiers currentModifiers;
    
    protected override void Start()
    {
        base.Start();
        
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        
        if (inventory != null)
            inventory.OnInventoryChanged += UpdateStats;
        
        UpdateStats();
    }
    
    private void UpdateStats()
    {
        if (inventory == null) return;
        
        currentModifiers = inventory.GetTotalStatModifiers();
        
        // Calculate modified stats
        int oldMaxHP = modifiedMaxHitPoints;
        modifiedMaxHitPoints = maximumHitPoints + currentModifiers.healthBonus;
        modifiedAttackPower = attackPower + currentModifiers.attackDamageBonus;
        modifiedMoveSpeed = moveSpeed * currentModifiers.movementSpeedMultiplier;
        modifiedAttackSpeed = defaultAttackCooldown / currentModifiers.attackSpeedMultiplier;
        defense = currentModifiers.defenseBonus;
        
        // Adjust current HP if max HP changed
        if (oldMaxHP != modifiedMaxHitPoints && oldMaxHP > 0)
        {
            float hpRatio = (float)currentHitPoints / oldMaxHP;
            currentHitPoints.value = Mathf.RoundToInt(modifiedMaxHitPoints * hpRatio);
        }
        else if (oldMaxHP == 0)
        {
            currentHitPoints.value = modifiedMaxHitPoints;
        }
    }
    
    public override bool TakeDamage(int damage, Entity damageOrigin)
    {
        // Apply defense reduction
        int reducedDamage = Mathf.Max(1, damage - defense);
        base.TakeDamage(reducedDamage, damageOrigin);
        return false;
    }
    
    public void AddItem(SO_ItemData itemData, int count = 1)
    {
        if (inventory != null)
            inventory.AddItem(itemData, count);
    }
    
    public void RemoveItem(SO_ItemData itemData, int count = 1)
    {
        if (inventory != null)
            inventory.RemoveItem(itemData, count);
    }
    
    // Getters for modified stats
    public int GetModifiedMaxHitPoints() => modifiedMaxHitPoints;
    public int GetModifiedAttackPower() => modifiedAttackPower;
    public float GetModifiedMoveSpeed() => modifiedMoveSpeed;
    public float GetModifiedAttackSpeed() => modifiedAttackSpeed;
    public int GetDefense() => defense;
    public StatModifiers GetCurrentModifiers() => currentModifiers;
    public Inventory GetInventory() => inventory;
}