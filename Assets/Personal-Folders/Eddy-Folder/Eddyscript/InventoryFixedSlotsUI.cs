using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class InventoryFixedSlotsUI : MonoBehaviour
{
    [Header("Inventory Source")]
    [SerializeField] private Inventory inventory;

    [Header("Fixed Slots (size must be 6)")]
    [SerializeField] private Image[] slotIcons = new Image[6];
    [SerializeField] private TextMeshProUGUI[] slotStackTexts = new TextMeshProUGUI[6];

    [Header("Fallback")]
    [SerializeField] private Sprite emptySprite;

    private void Awake()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        ClearSlots();

        if (inventory == null) return;

        List<Item> items = inventory.GetItems();
        int count = Mathf.Min(items.Count, 6);

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            if (slotIcons[i] != null)
            {
                slotIcons[i].enabled = true;
                slotIcons[i].sprite = item.itemData != null ? item.itemData.itemIcon : emptySprite;
            }

            if (slotStackTexts != null && i < slotStackTexts.Length && slotStackTexts[i] != null)
            {
                slotStackTexts[i].text = item.stackCount > 1 ? item.stackCount.ToString() : string.Empty;
            }
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < 6; i++)
        {
            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotIcons[i].sprite = emptySprite;
                slotIcons[i].enabled = emptySprite != null;
            }

            if (slotStackTexts != null && i < slotStackTexts.Length && slotStackTexts[i] != null)
            {
                slotStackTexts[i].text = string.Empty;
            }
        }
    }
}
