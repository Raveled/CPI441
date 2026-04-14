using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryFixedSlotsUI : MonoBehaviour
{
    [Header("Inventory Source")]
    [SerializeField] private Inventory inventory;

    [Header("Player Source")]
    [SerializeField] private Player player;

    [Header("Fixed Slots (size must be 6)")]
    [SerializeField] private Image[] slotIcons = new Image[6];
    [SerializeField] private Button[] slotButtons = new Button[6];
    [SerializeField] private TextMeshProUGUI[] slotStackTexts = new TextMeshProUGUI[6];

    [Header("Fallback")]
    [SerializeField] private Sprite emptySprite;

    private bool subscribed;

    private void Start()
    {
        ResolveRefs();
        BindButtons();
        Refresh();
    }

    private void Update()
    {
        if (inventory == null || player == null)
        {
            ResolveRefs();
        }

        if (inventory != null && !subscribed)
        {
            inventory.OnInventoryChanged += Refresh;
            subscribed = true;
            Refresh();
        }
    }

    private void OnDisable()
    {
        if (inventory != null && subscribed)
        {
            inventory.OnInventoryChanged -= Refresh;
            subscribed = false;
        }
    }

    private void ResolveRefs()
    {
        if (player == null)
        {
            Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (Player p in players)
            {
                if (p != null && p.isLocalPlayer())
                {
                    player = p;
                    break;
                }
            }

            if (player == null && players.Length > 0)
            {
                player = players[0];
            }
        }

        if (inventory == null && player != null)
        {
            inventory = player.GetComponent<Inventory>();

            if (inventory == null)
            {
                inventory = player.GetComponentInParent<Inventory>();
            }

            if (inventory == null)
            {
                inventory = player.GetComponentInChildren<Inventory>(true);
            }
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (inventory != null && !subscribed)
        {
            inventory.OnInventoryChanged += Refresh;
            subscribed = true;
        }
    }

    private void BindButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i;

            if (slotButtons[i] == null && i < slotIcons.Length && slotIcons[i] != null)
            {
                slotButtons[i] = slotIcons[i].GetComponent<Button>();
            }

            if (slotButtons[i] == null)
            {
                continue;
            }

            slotButtons[i].onClick.RemoveAllListeners();
            slotButtons[i].onClick.AddListener(() => SellSlot(slotIndex));
        }
    }

    public void Refresh()
    {
        ClearSlots();

        if (inventory == null)
        {
            return;
        }

        var items = inventory.GetItems();
        int count = Mathf.Min(items.Count, slotIcons.Length);

        for (int i = 0; i < count; i++)
        {
            Item item = items[i];
            if (item == null || item.itemData == null)
            {
                continue;
            }

            if (slotIcons[i] != null)
            {
                slotIcons[i].sprite = item.itemData.itemIcon != null ? item.itemData.itemIcon : emptySprite;
                slotIcons[i].enabled = slotIcons[i].sprite != null;
                slotIcons[i].color = Color.white;
                slotIcons[i].preserveAspect = true;
            }

            if (i < slotStackTexts.Length && slotStackTexts[i] != null)
            {
                slotStackTexts[i].text = item.stackCount > 1 ? item.stackCount.ToString() : string.Empty;
            }
        }
    }

    private void SellSlot(int index)
    {
        ResolveRefs();

        if (inventory == null || player == null)
        {
            return;
        }

        Item item = inventory.GetItemAt(index);
        if (item == null || item.itemData == null)
        {
            return;
        }

        int sellValue = Mathf.Max(1, item.itemData.cost / 2);
        bool removed = inventory.TryRemoveAt(index, 1);
        if (!removed)
        {
            return;
        }

        player.IncreaseGoldTotal(sellValue);
        Refresh();
    }

    private void ClearSlots()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotIcons[i] != null)
            {
                slotIcons[i].sprite = emptySprite;
                slotIcons[i].enabled = emptySprite != null;
                slotIcons[i].color = Color.white;
                slotIcons[i].preserveAspect = true;
            }

            if (i < slotStackTexts.Length && slotStackTexts[i] != null)
            {
                slotStackTexts[i].text = string.Empty;
            }
        }
    }
}