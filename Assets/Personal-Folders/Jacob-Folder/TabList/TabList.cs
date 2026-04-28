using System;
using System.Collections.Generic;
using System.Linq;
using PurrLobby;
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class TabList : NetworkBehaviour
{
    [SerializeField] private TabListEntry tabListEntryPrefab;
    [SerializeField] private int startY = 35;

    public SyncList<TabListEntry> entries = new SyncList<TabListEntry>();

    private CanvasGroup canvasGroup;
    public InputAction tabAction;
    private LobbyPlayerRegistry lobbyPlayerRegistry;

    protected override void OnSpawned(bool asServer)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        lobbyPlayerRegistry = FindAnyObjectByType<LobbyPlayerRegistry>();

        if (asServer)
        {
            StartCoroutine(ServerInitialize());
        }
        else
        {
            // Only use this to add the local player to the list, not necessary over steam network since player reg will be populated in the lobby
            if (lobbyPlayerRegistry == null)
            {
                StartCoroutine(ClientInitialize());
            }
        }

        tabAction = InputSystem.actions.FindAction("Tab");
        tabAction?.Enable();
        tabAction.started += OnTabPressed();
    }

    private IEnumerator<WaitForSeconds> ServerInitialize()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("Initializing Tab List on Server...");

        List<GameManager.PlayerInfo> playerInfo = GameManager.Instance.GetPlayerInfos();

        if (lobbyPlayerRegistry != null)
        {
            List<LobbyUser> lobbyUsers = lobbyPlayerRegistry.GetPlayers();
            foreach (LobbyUser lobbyUser in lobbyUsers)
            {
                foreach (GameManager.PlayerInfo info in playerInfo)
                {
                    if (lobbyUser.Id == info.steamID.ToString())
                    {
                        TabListEntry entry = Instantiate(tabListEntryPrefab, transform);
                        entry.Init(lobbyUser.Id, info.playerID, lobbyUser.DisplayName, lobbyUser.Avatar, lobbyUser.Team, lobbyUser.Character);

                        entries.Add(entry);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Lobby registry not found. Is Steam initialized?");

            foreach (GameManager.PlayerInfo info in playerInfo)
            {
                TabListEntry entry = Instantiate(tabListEntryPrefab, transform);
                entry.Init(null, info.playerID, info.playerID.ToString(), null, (int) info.team, info.character);

                entries.Add(entry);
            }
        }

        SortEntries();
    }

    public void SortEntries()
    {
        var sortedEntries = entries.OrderBy(e => e.team.value).ThenBy(e => e.character.value).ToList();
        for (int i = 0; i < sortedEntries.Count; i++)
        {
            var entry = sortedEntries[i];
            entry.transform.localPosition = new Vector3(0, startY - (i * (entry.GetHeight()+2)), 0);
        }
    }

    private IEnumerator<WaitForSeconds> ClientInitialize()
    {
        yield return new WaitForSeconds(1f);

        if (!isServer)
        {
            Debug.Log("Initializing Tab List Entry for Client...");
            PlayerID localPlayerID = networkManager.localPlayer;

            AddEntryServerRpc(localPlayerID);
        }
    }

    [ServerRpc]
    private void AddEntryServerRpc(PlayerID playerID)
    {
        Debug.Log("[TAB LIST] SERVER RPC: Adding a new entry");
        if (entries.Any(e => e.playerID.value == playerID))
            return;
        
        GameManager.PlayerInfo? playerInfo = GameManager.Instance.GetPlayerConfiguration(playerID);
        if (playerInfo != null)
        {
            GameManager.PlayerInfo info = (GameManager.PlayerInfo) playerInfo;
            TabListEntry entry = Instantiate(tabListEntryPrefab, transform);
            entry.Init(null, info.playerID, info.playerID.ToString(), null, (int) info.team, info.character);

            entries.Add(entry);
            SortEntries();
        }
    }

    private Action<InputAction.CallbackContext> OnTabPressed()
    {
        return context =>
        {
            if (context.started)
            {
                bool isVisible = canvasGroup.alpha > 0f;
                canvasGroup.alpha = isVisible ? 0f : 1f;
            }
        };
    }
}
