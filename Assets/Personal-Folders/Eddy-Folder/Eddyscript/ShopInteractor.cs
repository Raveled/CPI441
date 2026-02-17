using UnityEngine;
[RequireComponent(typeof(Collider))]
public sealed class ShopInteractor : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool canInteract;

    private void Awake()
    {
        if (catalog == null) catalog = GetComponent<ShopCatalog>();
        if (shopUI == null) shopUI = FindObjectOfType<ShopUI>();

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (!canInteract || shopUI == null || catalog == null) return;
        if (Input.GetKeyDown(interactKey)) shopUI.Toggle(catalog);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) canInteract = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        canInteract = false;
        if (shopUI != null) shopUI.Close();
    }
}