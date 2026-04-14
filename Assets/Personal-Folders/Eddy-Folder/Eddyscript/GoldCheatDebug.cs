using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GoldCheatDebug : MonoBehaviour
{
    [SerializeField] private int goldPerPress = 20;
    [SerializeField] private Key cheatKey = Key.P;
    [SerializeField] private Player player;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        ResolveLocalPlayer();

        if (player == null)
        {
            return;
        }

        if (keyboard[cheatKey].wasPressedThisFrame)
        {
            player.IncreaseGoldTotal(goldPerPress);
            Debug.Log($"[GoldCheatDebug] Added {goldPerPress} gold. Total gold: {player.GetGoldTotal()}");
        }
    }

    private void ResolveLocalPlayer()
    {
        if (player != null)
        {
            return;
        }

        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player p in players)
        {
            if (p != null && p.isLocalPlayer())
            {
                player = p;
                return;
            }
        }

        if (players.Length > 0)
        {
            player = players[0];
        }
    }
}