using UnityEngine;

[System.Serializable]
public class Item
{
    public SO_ItemData itemData;
    public int stackCount = 1;
    
    public Item(SO_ItemData data, int count = 1)
    {
        itemData = data;
        stackCount = Mathf.Clamp(count, 1, data.maxStacks);
    }
    
    public bool CanStack(Item other)
    {
        return itemData == other.itemData && itemData.stackable && stackCount < itemData.maxStacks;
    }
    
    public void AddStack(int amount = 1)
    {
        stackCount = Mathf.Clamp(stackCount + amount, 1, itemData.maxStacks);
    }
}