using TMPro;
using UnityEngine;

public sealed class GoldCounterUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private string prefix = "Gold: ";

    private void Awake()
    {
        if (goldText == null)
        {
            goldText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        ResolvePlayer();

        if (goldText == null)
        {
            return;
        }

        goldText.text = player != null ? $"{prefix}{player.GetGoldTotal()}" : $"{prefix}0";
    }

    private void ResolvePlayer()
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