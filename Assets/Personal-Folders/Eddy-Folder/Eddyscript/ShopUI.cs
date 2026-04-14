using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ShopUI : MonoBehaviour
{
    public static bool IsAnyOpen { get; private set; }

    [Header("UI Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemButtonUI itemButtonPrefab;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Optional")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private Player player;

    private readonly List<GameObject> spawned = new();
    private ShopCatalog currentCatalog;

    private void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }

        ResolvePlayerRefs();
        SetOpen(false);
    }

    private void Update()
    {
        if (!root.activeSelf)
        {
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && kb[Key.Escape].wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Toggle(ShopCatalog catalog)
    {
        if (root.activeSelf)
        {
            Close();
        }
        else
        {
            Open(catalog);
        }
    }

    public void Open(ShopCatalog catalog)
    {
        currentCatalog = catalog;
        ResolvePlayerRefs();
        Rebuild();
        SetStatus(string.Empty);
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
        currentCatalog = null;
        SetStatus(string.Empty);
    }

    public void CloseFromButton()
    {
        Close();
    }

    private void ResolvePlayerRefs()
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

        if (playerInventory == null && player != null)
        {
            playerInventory = player.GetComponent<Inventory>();

            if (playerInventory == null)
            {
                playerInventory = player.GetComponentInParent<Inventory>();
            }

            if (playerInventory == null)
            {
                playerInventory = player.GetComponentInChildren<Inventory>(true);
            }
        }

        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<Inventory>();
        }
    }

    private void Rebuild()
    {
        Clear();

        if (currentCatalog == null || itemsContainer == null || itemButtonPrefab == null)
        {
            return;
        }

        var items = currentCatalog.ItemsForSale;
        foreach (SO_ItemData item in items)
        {
            if (item == null)
            {
                continue;
            }

            ShopItemButtonUI entry = Instantiate(itemButtonPrefab, itemsContainer);
            entry.Bind(item, TryBuy);
            spawned.Add(entry.gameObject);
        }
    }

    private void TryBuy(SO_ItemData item)
    {
        if (item == null)
        {
            SetStatus("Invalid item.");
            return;
        }

        ResolvePlayerRefs();

        if (player == null)
        {
            SetStatus("Player not found.");
            return;
        }

        if (playerInventory == null)
        {
            SetStatus("Inventory not found.");
            return;
        }

        if (!playerInventory.CanAddItem(item, 1))
        {
            SetStatus(item.stackable ? "Inventory full." : "Already owned.");
            return;
        }

        if (!player.TrySpendGold(item.cost))
        {
            SetStatus("Not enough gold.");
            return;
        }

        bool ok = playerInventory.AddItem(item, 1);
        if (!ok)
        {
            player.IncreaseGoldTotal(item.cost);
            SetStatus(item.stackable ? "Inventory full." : "Already owned.");
            return;
        }

        SetStatus($"Bought: {item.itemName} (-{item.cost}g)");
    }

    private void Clear()
    {
        foreach (var go in spawned)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }

        spawned.Clear();
    }

    private void SetOpen(bool open)
    {
        IsAnyOpen = open;
        root.SetActive(open);
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
        {
            statusText.text = msg ?? string.Empty;
        }
    }
}