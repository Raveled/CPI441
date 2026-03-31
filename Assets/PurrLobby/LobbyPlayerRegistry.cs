using System.Collections.Generic;
using PurrLobby;
using UnityEngine;

public class LobbyPlayerRegistry : MonoBehaviour
{
    public static LobbyPlayerRegistry Instance { get; private set; }

    [SerializeField] private List<LobbyUser> players = new();

    public IReadOnlyList<LobbyUser> Players => players;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnLobbyUpdated(Lobby lobby)
    {
        if (!lobby.IsValid) return;

        // Update existing or add new
        foreach (var member in lobby.Members)
        {
            int idx = players.FindIndex(x => x.Id == member.Id);
            if (idx >= 0)
                players[idx] = member;
            else
                players.Add(member);
        }

        // Remove players who have left
        players.RemoveAll(x => !lobby.Members.Exists(m => m.Id == x.Id));
    }

    public bool TryGetPlayer(string userId, out LobbyUser user)
    {
        int idx = players.FindIndex(x => x.Id == userId);
        if (idx >= 0)
        {
            user = players[idx];
            return true;
        }
        user = default;
        return false;
    }

    public void Clear() => players.Clear();

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
