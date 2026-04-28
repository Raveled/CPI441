using System.Collections;
using UnityEngine;

public sealed class InventoryStatApplier : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private Player player;

    private bool subscribed;

    private void Awake()
    {
        if (player == null)
            player = GetComponent<Player>() ?? GetComponentInParent<Player>() ?? GetComponentInChildren<Player>(true);

        if (inventory == null)
            inventory = GetComponent<Inventory>() ?? GetComponentInParent<Inventory>() ?? GetComponentInChildren<Inventory>(true);
    }

    private void OnEnable()
    {
        StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        if (inventory != null && subscribed)
        {
            inventory.OnInventoryChanged -= ApplyInventoryStats;
            subscribed = false;
        }
    }

    private IEnumerator BindWhenReady()
    {
        while (player == null || inventory == null)
        {
            if (player == null)
                player = GetComponent<Player>() ?? GetComponentInParent<Player>() ?? GetComponentInChildren<Player>(true);

            if (inventory == null)
                inventory = GetComponent<Inventory>() ?? GetComponentInParent<Inventory>() ?? GetComponentInChildren<Inventory>(true);

            yield return null;
        }

        while (!player.isSpawned)
        {
            yield return null;
        }

        if (!subscribed)
        {
            inventory.OnInventoryChanged += ApplyInventoryStats;
            subscribed = true;
        }

        ApplyInventoryStats();
    }

    private void ApplyInventoryStats()
    {
        if (inventory == null || player == null || !player.isSpawned)
            return;

        StatModifiers mods = inventory.GetTotalStatModifiers();

        Debug.Log(
            $"[InventoryStatApplier] Applying mods to {player.name} | " +
            $"HP+={mods.healthBonus}, " +
            $"ATK+={mods.attackDamageBonus}, " +
            $"ASx={mods.attackSpeedMultiplier}, " +
            $"MSx={mods.movementSpeedMultiplier}, " +
            $"DEF+={mods.defenseBonus}"
        );

        player.SetInventoryModifiers(mods);
    }
}