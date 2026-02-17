using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Items/Item Data")]
public class SO_ItemData : ScriptableObject
{
    [Header("Item Info")]
    public string itemName = "New Item";
    public Sprite itemIcon;
    [TextArea(3, 5)]
    public string description = "";
    
    [Header("Stat Modifiers")]
    public int healthBonus = 0;
    public int attackDamageBonus = 0;
    public float attackSpeedMultiplier = 1f;
    public float movementSpeedMultiplier = 1f;
    public int defenseBonus = 0;
    
    [Header("Special Effects")]
    public bool stackable = true;
    public int maxStacks = 1;
}