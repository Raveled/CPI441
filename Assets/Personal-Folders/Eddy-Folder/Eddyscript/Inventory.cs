using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class Inventory : MonoBehaviour
{
    [SerializeField] private List<Item> items = new();

    public event Action OnInventoryChanged;

    public List<Item> GetItems()
    {
        return items;
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
            if (item != null && item.itemData == itemData)
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
            return !HasItem(itemData);
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData != itemData)
            {
                continue;
            }

            if (item.stackCount < itemData.maxStacks)
            {
                return true;
            }
        }

        return true;
    }

    public bool AddItem(SO_ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        if (!itemData.stackable)
        {
            if (HasItem(itemData))
            {
                return false;
            }

            items.Add(new Item(itemData, 1));
            OnInventoryChanged?.Invoke();
            return true;
        }

        int remaining = amount;

        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData != itemData)
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

        while (remaining > 0)
        {
            int toAdd = Mathf.Min(itemData.maxStacks, remaining);
            items.Add(new Item(itemData, toAdd));
            remaining -= toAdd;
        }

        OnInventoryChanged?.Invoke();
        return true;
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
    public bool RemoveItem(SO_ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData != itemData)
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
}