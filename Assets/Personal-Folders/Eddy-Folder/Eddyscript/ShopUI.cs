using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopUI : MonoBehaviour
{
    public static bool IsAnyOpen { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Item List")]
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemButtonUI itemButtonPrefab;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Optional Player Refs")]
    [SerializeField] private Player player;
    [SerializeField] private Inventory playerInventory;

    private readonly List<GameObject> spawned = new();
    private ShopCatalog currentCatalog;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        if (root != null)
            root.SetActive(false);

        IsAnyOpen = false;
        SetStatus(string.Empty);
    }

    public void Open(ShopCatalog catalog)
    {
        currentCatalog = catalog;

        if (root != null)
            root.SetActive(true);

        IsAnyOpen = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Rebuild();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);

        IsAnyOpen = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SetStatus(string.Empty);
    }

    public void Toggle(ShopCatalog catalog)
    {
        if (IsOpen())
            Close();
        else
            Open(catalog);
    }

    public bool IsOpen()
    {
        return root != null && root.activeSelf;
    }

    public void Rebuild()
    {
        Clear();

        if (currentCatalog == null)
        {
            SetStatus("No shop catalog assigned.");
            return;
        }

        if (itemsContainer == null)
        {
            SetStatus("Items container is missing.");
            return;
        }

        if (itemButtonPrefab == null)
        {
            SetStatus("Item button prefab is missing.");
            return;
        }

        IReadOnlyList<SO_ItemData> items = currentCatalog.ItemsForSale;
        if (items == null || items.Count == 0)
        {
            SetStatus("No items for sale.");
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            SO_ItemData item = items[i];
            if (item == null)
                continue;

            ShopItemButtonUI entry = Instantiate(itemButtonPrefab, itemsContainer);
            entry.Bind(item, TryBuy);
            spawned.Add(entry.gameObject);
        }

        SetStatus(string.Empty);
    }

    public void Clear()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }

        spawned.Clear();
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

        if (!player.TrySpendGold(item.cost))
        {
            SetStatus("Not enough gold.");
            return;
        }

        bool added = playerInventory.AddItem(item, 1);
        if (!added)
        {
            player.IncreaseGoldTotal(item.cost);

            if (!item.stackable && playerInventory.HasItem(item))
                SetStatus("Already owned.");
            else
                SetStatus("Inventory full.");

            return;
        }

        SetStatus($"Bought: {item.itemName} (-{item.cost}g)");
    }

    private void ResolvePlayerRefs()
    {
        if (player == null)
        {
            Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].isLocalPlayer())
                {
                    player = players[i];
                    break;
                }
            }
        }

        if (playerInventory == null && player != null)
        {
            playerInventory = player.GetComponent<Inventory>();
            if (playerInventory == null)
                playerInventory = player.GetComponentInChildren<Inventory>(true);
            if (playerInventory == null)
                playerInventory = player.GetComponentInParent<Inventory>();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void OnDisable()
    {
        if (IsAnyOpen)
            IsAnyOpen = false;
    }
}