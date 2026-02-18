using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private List<Item> items = new List<Item>();
    [SerializeField] private int maxSlots = 6;
    
    public System.Action OnInventoryChanged;
    
    public bool AddItem(SO_ItemData itemData, int count = 1)
    {
        // Try to stack with existing item
        foreach (Item item in items)
        {
            if (item.itemData == itemData && item.itemData.stackable && item.stackCount < item.itemData.maxStacks)
            {
                int canAdd = Mathf.Min(count, item.itemData.maxStacks - item.stackCount);
                item.AddStack(canAdd);
                count -= canAdd;
                if (count <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        // Create new item stacks
        while (count > 0 && items.Count < maxSlots)
        {
            int stackSize = Mathf.Min(count, itemData.maxStacks);
            items.Add(new Item(itemData, stackSize));
            count -= stackSize;
        }
        
        OnInventoryChanged?.Invoke();
        return count <= 0;
    }
    
    public bool RemoveItem(SO_ItemData itemData, int count = 1)
    {
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].itemData == itemData)
            {
                int removeCount = Mathf.Min(count, items[i].stackCount);
                items[i].stackCount -= removeCount;
                count -= removeCount;
                
                if (items[i].stackCount <= 0)
                    items.RemoveAt(i);
                
                if (count <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }
        
        OnInventoryChanged?.Invoke();
        return false;
    }
    
    public StatModifiers GetTotalStatModifiers()
    {
        StatModifiers total = new StatModifiers();
        
        foreach (Item item in items)
        {
            total.healthBonus += item.itemData.healthBonus * item.stackCount;
            total.attackDamageBonus += item.itemData.attackDamageBonus * item.stackCount;
            total.attackSpeedMultiplier *= Mathf.Pow(item.itemData.attackSpeedMultiplier, item.stackCount);
            total.movementSpeedMultiplier *= Mathf.Pow(item.itemData.movementSpeedMultiplier, item.stackCount);
            total.defenseBonus += item.itemData.defenseBonus * item.stackCount;
        }
        
        return total;
    }
    
    public List<Item> GetItems() => items;
    public int GetItemCount(SO_ItemData itemData)
    {
        int count = 0;
        foreach (Item item in items)
        {
            if (item.itemData == itemData)
                count += item.stackCount;
        }
        return count;
    }
}

[System.Serializable]
public struct StatModifiers
{
    public int healthBonus;
    public int attackDamageBonus;
    public float attackSpeedMultiplier;
    public float movementSpeedMultiplier;
    public int defenseBonus;
    
    public StatModifiers(int health = 0, int attack = 0, float attackSpeed = 1f, float moveSpeed = 1f, int defense = 0)
    {
        healthBonus = health;
        attackDamageBonus = attack;
        attackSpeedMultiplier = attackSpeed;
        movementSpeedMultiplier = moveSpeed;
        defenseBonus = defense;
    }
}