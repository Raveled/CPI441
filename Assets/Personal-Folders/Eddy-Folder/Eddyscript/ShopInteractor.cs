using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public sealed class ShopInteractor : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private ShopUI shopUI;

    [Header("New Input System Key")]
    [SerializeField] private Key interactKey = Key.G;

    private bool canInteract;

    private void Awake()
    {
        if (catalog == null) catalog = GetComponent<ShopCatalog>();
        if (shopUI == null) shopUI = FindFirstObjectByType<ShopUI>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;

        Debug.Log($"[Shop] Awake '{name}' catalog={(catalog ? "OK" : "NULL")} shopUI={(shopUI ? "OK" : "NULL")}");
    }

    private void Update()
    {
        if (!canInteract || shopUI == null || catalog == null) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[interactKey].wasPressedThisFrame)
            shopUI.Toggle(catalog);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            canInteract = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        canInteract = false;
        shopUI?.Close();
    }
}