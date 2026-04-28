using System;
using System.Collections.Generic;
using System.Linq;
using PurrLobby;
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public class TabList : NetworkBehaviour
{
    [SerializeField] private TabListEntry tabListEntryPrefab;
    [SerializeField] private int startY = 25;

    public SyncList<TabListEntry> entries = new SyncList<TabListEntry>();

    private CanvasGroup canvasGroup;
    public InputAction tabAction;

    protected override void OnSpawned(bool asServer)
    {
        StartCoroutine(DelayedSpawn(asServer));
    }

    private IEnumerator<WaitForSeconds> DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(1f);

        if (asServer)
        {
            Debug.Log("Initializing Tab List...");
            List<GameManager.PlayerInfo> playerInfo = GameManager.Instance.GetPlayerInfos();

            LobbyPlayerRegistry lobbyPlayerRegistry = FindAnyObjectByType<LobbyPlayerRegistry>();
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

            var sortedEntries = entries.OrderBy(e => e.team).ThenBy(e => e.character).ToList();
            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var entry = sortedEntries[i];
                entry.transform.localPosition = new Vector3(0, startY - (i * entry.GetHeight()), 0);
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            tabAction = InputSystem.actions.FindAction("Tab");
            tabAction?.Enable();
            tabAction.started += OnTabPressed();
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

    // Update is called once per frame
    void Update()
    {
        foreach (TabListEntry entry in entries)
        {
            if (entry == null) continue;
            entry.UpdateEntry();
        }
    }
}
