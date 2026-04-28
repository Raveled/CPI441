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
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
            return;

        if (localPlayerInRange == player)
            localPlayerInRange = null;
    }

    private void Update()
    {
        if (localPlayerInRange == null || shopUI == null || catalog == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        bool pressed = interactKey switch
        {
            Key.G => keyboard.gKey.wasPressedThisFrame,
            Key.E => keyboard.eKey.wasPressedThisFrame,
            Key.F => keyboard.fKey.wasPressedThisFrame,
            _ => false
        };

        if (!pressed)
            return;

        shopUI.Toggle(catalog);
    }
}