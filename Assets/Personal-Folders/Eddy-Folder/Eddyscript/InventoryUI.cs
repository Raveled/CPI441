using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private TextMeshProUGUI statsText;
    
    [Header("Player Reference")]
    [SerializeField] private EnhancedPlayer player;
    
    private List<GameObject> itemSlots = new List<GameObject>();
    
    private void Start()
    {
        if (player == null)
            player = FindObjectOfType<EnhancedPlayer>();
        
        if (player != null && player.GetInventory() != null)
        {
            player.GetInventory().OnInventoryChanged += UpdateUI;
            UpdateUI();
        }
    }
    
    private void UpdateUI()
    {
        ClearSlots();
        
        if (player == null || player.GetInventory() == null) return;
        
        List<Item> items = player.GetInventory().GetItems();
        
        foreach (Item item in items)
        {
            CreateItemSlot(item);
        }
        
        UpdateStatsDisplay();
    }
    
    private void CreateItemSlot(Item item)
    {
        if (itemSlotPrefab == null || itemContainer == null) return;
        
        GameObject slot = Instantiate(itemSlotPrefab, itemContainer);
        itemSlots.Add(slot);
        
        // Set up the slot (assuming it has Image and Text components)
        Image icon = slot.GetComponentInChildren<Image>();
        TextMeshProUGUI stackText = slot.GetComponentInChildren<TextMeshProUGUI>();
        
        if (icon != null && item.itemData.itemIcon != null)
            icon.sprite = item.itemData.itemIcon;
        
        if (stackText != null && item.stackCount > 1)
            stackText.text = item.stackCount.ToString();
        else if (stackText != null)
            stackText.text = "";
    }
    
    private void ClearSlots()
    {
        foreach (GameObject slot in itemSlots)
        {
            if (slot != null)
                DestroyImmediate(slot);
        }
        itemSlots.Clear();
    }
    
    private void UpdateStatsDisplay()
    {
        if (statsText == null || player == null) return;
        
        StatModifiers mods = player.GetCurrentModifiers();
        
        string statsDisplay = "Current Bonuses:\n";
        if (mods.healthBonus != 0)
            statsDisplay += $"Health: +{mods.healthBonus}\n";
        if (mods.attackDamageBonus != 0)
            statsDisplay += $"Attack Damage: +{mods.attackDamageBonus}\n";
        if (mods.attackSpeedMultiplier != 1f)
            statsDisplay += $"Attack Speed: x{mods.attackSpeedMultiplier:F2}\n";
        if (mods.movementSpeedMultiplier != 1f)
            statsDisplay += $"Movement Speed: x{mods.movementSpeedMultiplier:F2}\n";
        if (mods.defenseBonus != 0)
            statsDisplay += $"Defense: +{mods.defenseBonus}\n";
        
        if (statsDisplay == "Current Bonuses:\n")
            statsDisplay = "No active bonuses";
        
        statsText.text = statsDisplay;
    }
}