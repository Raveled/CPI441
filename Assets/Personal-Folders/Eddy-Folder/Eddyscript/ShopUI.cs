using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ShopUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Transform itemsContainer;
    [SerializeField] private ShopItemButtonUI itemButtonPrefab;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Optional")]
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
        if (!root.activeSelf) return;

        var kb = Keyboard.current;
        if (kb != null && kb[Key.Escape].wasPressedThisFrame)
            Close();
    }

    public void Toggle(ShopCatalog catalog)
    {
        if (root.activeSelf) Close();
        else Open(catalog);
    }

    public void Open(ShopCatalog catalog)
    {
        currentCatalog = catalog;

        if (playerInventory == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) player.TryGetComponent(out playerInventory);
        }

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
        Debug.Log("[ShopUI] CloseFromButton clicked");
        Close();
    }

    private void Rebuild()
    {
        Clear();

        if (currentCatalog == null)
        {
            Debug.LogWarning("[ShopUI] Rebuild called with NULL catalog.");
            return;
        }

        if (itemsContainer == null || itemButtonPrefab == null)
        {
            Debug.LogWarning($"[ShopUI] Missing refs. itemsContainer={(itemsContainer ? "OK" : "NULL")} itemButtonPrefab={(itemButtonPrefab ? "OK" : "NULL")}");
            return;
        }

        var items = currentCatalog.ItemsForSale;
        Debug.Log($"[ShopUI] Building {items.Count} items.");

        foreach (var item in items)
        {
            if (item == null) continue;
            var entry = Instantiate(itemButtonPrefab, itemsContainer);
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

        if (playerInventory == null)
        {
            SetStatus("Player inventory not found.");
            return;
        }

        bool ok = playerInventory.AddItem(item, 1);
        SetStatus(ok ? $"Bought: {item.itemName}" : "Inventory full.");
    }

    private void Clear()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
    }

    private void SetOpen(bool open)
    {
        root.SetActive(open);
        Cursor.visible = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg ?? string.Empty;
    }
}