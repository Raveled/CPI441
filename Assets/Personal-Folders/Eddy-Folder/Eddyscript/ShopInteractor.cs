using UnityEngine;
using UnityEngine.InputSystem;

public class ShopInteractor : MonoBehaviour
{
    [SerializeField] private ShopCatalog catalog;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private Key interactKey = Key.G;

    private Player localPlayerInRange;

    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null || !player.isLocalPlayer())
            return;

        localPlayerInRange = player;
        Debug.Log($"[ShopInteractor] Player entered shop: {player.name} via collider {other.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        if (localPlayerInRange == player)
        {
            localPlayerInRange = null;
            Debug.Log($"[ShopInteractor] Player exited shop: {player.name} via collider {other.name}");
        }
    }

    private void Update()
    {
        if (localPlayerInRange == null || shopUI == null || catalog == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (IsInteractPressed(keyboard))
        {
            shopUI.Open(catalog);
            Debug.Log($"[ShopInteractor] Opened shop for {localPlayerInRange.name}");
        }
    }

    private bool IsInteractPressed(Keyboard keyboard)
    {
        return interactKey switch
        {
            Key.G => keyboard.gKey.wasPressedThisFrame,
            Key.E => keyboard.eKey.wasPressedThisFrame,
            Key.Q => keyboard.qKey.wasPressedThisFrame,
            Key.R => keyboard.rKey.wasPressedThisFrame,
            Key.F => keyboard.fKey.wasPressedThisFrame,
            _ => false
        };
    }
}