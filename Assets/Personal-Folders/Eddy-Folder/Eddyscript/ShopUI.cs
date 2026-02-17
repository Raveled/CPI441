// File: Assets/Scripts/Shop/ShopUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class ShopUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemButtonUI itemButtonPrefab;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Optional (can be left empty if player spawns at runtime)")]
    [SerializeField] private Inventory playerInventory;

    private readonly List<GameObject> spawned = new();
    private ShopCatalog currentCatalog;

    private void Awake()
    {
        if (root == null) root = gameObject;
        SetOpen(false);
    }

    private void Update()
    {
        if (root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open(ShopCatalog catalog)
    {
        currentCatalog = catalog;

        EnsureInventoryReference();
        Build();

        SetOpen(true);
        SetStatus(string.Empty);
    }

    public void Close()
    {
        SetOpen(false);
        currentCatalog = null;
        SetStatus(string.Empty);
    }

    public void Toggle(ShopCatalog catalog)
    {
        if (root.activeSelf) Close();
        else Open(catalog);
    }

    private void EnsureInventoryReference()
    {
        if (playerInventory != null) return;

        // If you tag your spawned player as "Player", this is reliable.
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent(out Inventory inv))
        {
            playerInventory = inv;
            return;
        }

        // Fallback: just find any inventory (works if only player has Inventory)
        playerInventory = FindObjectOfType<Inventory>();
    }

    private void SetOpen(bool open)
    {
        root.SetActive(open);

        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void Build()
    {
        Clear();

        if (currentCatalog == null || itemsContainer == null || itemButtonPrefab == null)
            return;

        foreach (var item in currentCatalog.ItemsForSale)
        {
            var entry = Instantiate(itemButtonPrefab, itemsContainer);
            entry.Bind(item, TryBuy);
            spawned.Add(entry.gameObject);
        }
    }

    private void Clear()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }

    private void TryBuy(SO_ItemData item)
    {
        EnsureInventoryReference();

        if (playerInventory == null)
        {
            SetStatus("Player inventory not found.");
            return;
        }

        bool ok = playerInventory.AddItem(item, 1);
        SetStatus(ok ? $"Bought: {item.itemName}" : "Inventory full (6 slots).");
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message ?? string.Empty;
    }
}
