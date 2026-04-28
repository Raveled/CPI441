using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Inventory : MonoBehaviour
{
    [SerializeField] private List<Item> items = new();
    [SerializeField] private int maxSlots = 6;

    public event Action OnInventoryChanged;

    public List<Item> GetItems()
    {
        return items;
    }

    public int GetMaxSlots()
    {
        return maxSlots;
    }

    public Item GetItemAt(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }

    public bool HasItem(SO_ItemData itemData)
    {
        if (itemData == null)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData == null)
            {
                continue;
            }

            if (IsSameItem(item.itemData, itemData))
            {
                return true;
            }
        }

        return false;
    }

    public bool CanAddItem(SO_ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        if (!itemData.stackable)
        {
            return !HasItem(itemData) && items.Count < maxSlots;
        }

        int remaining = amount;

        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData == null || !IsSameItem(item.itemData, itemData))
            {
                continue;
            }

            int freeSpace = Mathf.Max(0, itemData.maxStacks - item.stackCount);
            remaining -= freeSpace;
        }

        if (remaining <= 0)
        {
            return true;
        }

        int stacksNeeded = Mathf.CeilToInt((float)remaining / Mathf.Max(1, itemData.maxStacks));
        return items.Count + stacksNeeded <= maxSlots;
    }

    public bool AddItem(SO_ItemData itemData, int amount)
    {
        if (!CanAddItem(itemData, amount))
        {
            return false;
        }

        if (!itemData.stackable)
        {
            items.Add(new Item(itemData, 1));
            OnInventoryChanged?.Invoke();
            return true;
        }

        int remaining = amount;

        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData == null || !IsSameItem(item.itemData, itemData))
            {
                continue;
            }

            int space = itemData.maxStacks - item.stackCount;
            if (space <= 0)
            {
                continue;
            }

            int toAdd = Mathf.Min(space, remaining);
            item.stackCount += toAdd;
            remaining -= toAdd;
        }

        while (remaining > 0 && items.Count < maxSlots)
        {
            int toAdd = Mathf.Min(itemData.maxStacks, remaining);
            items.Add(new Item(itemData, toAdd));
            remaining -= toAdd;
        }

        bool success = remaining <= 0;
        if (success)
        {
            OnInventoryChanged?.Invoke();
        }

        return success;
    }

    public bool TryRemoveAt(int index, int amount = 1)
    {
        if (index < 0 || index >= items.Count || amount <= 0)
        {
            return false;
        }

        Item item = items[index];
        if (item == null)
        {
            return false;
        }

        if (item.stackCount > amount)
        {
            item.stackCount -= amount;
        }
        else
        {
            items.RemoveAt(index);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(SO_ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData == null || !IsSameItem(item.itemData, itemData))
            {
                continue;
            }

            if (item.stackCount > amount)
            {
                item.stackCount -= amount;
            }
            else
            {
                items.RemoveAt(i);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        return false;
    }

    public bool RemoveItemAt(int index, int amount = 1)
    {
        return TryRemoveAt(index, amount);
    }

    public StatModifiers GetTotalStatModifiers()
    {
        StatModifiers total = new StatModifiers
        {
            healthBonus = 0,
            attackDamageBonus = 0,
            attackSpeedMultiplier = 1f,
            movementSpeedMultiplier = 1f,
            defenseBonus = 0
        };

        foreach (Item item in items)
        {
            if (item == null || item.itemData == null)
            {
                continue;
            }

            total.healthBonus += item.itemData.healthBonus * item.stackCount;
            total.attackDamageBonus += item.itemData.attackDamageBonus * item.stackCount;
            total.attackSpeedMultiplier *= Mathf.Pow(item.itemData.attackSpeedMultiplier, item.stackCount);
            total.movementSpeedMultiplier *= Mathf.Pow(item.itemData.movementSpeedMultiplier, item.stackCount);
            total.defenseBonus += item.itemData.defenseBonus * item.stackCount;
        }

        return total;
    }

    private bool IsSameItem(SO_ItemData a, SO_ItemData b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a == b)
        {
            return true;
        }

        return string.Equals(a.itemName, b.itemName, StringComparison.Ordinal);
    }
}