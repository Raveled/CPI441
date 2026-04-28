using System.Collections.Generic;
using PurrLobby;
using PurrNet;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TabListEntry : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image characterIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI deathsText;
    [SerializeField] private List<Sprite> characterIcons;

    public SyncVar<string> steamID;
    public SyncVar<PlayerID> playerID;
    public SyncVar<string> displayName;
    public Texture2D avatar;
    public SyncVar<int> team;
    public SyncVar<string> character;
    public SyncVar<int> kills;
    public SyncVar<int> deaths;

    public void Init(string steamID, PlayerID playerID, string displayName, int team, string character)
    {
        this.steamID.value = steamID;
        this.playerID.value = playerID;
        this.displayName.value = displayName;
        this.team.value = team;
        this.character.value = character;
        this.kills.value = 0;
        this.deaths.value = 0;
        this.avatar = null;
    }

    private void Update()
    {
        if (isServer)
        {
            Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (Player p in players)
            {
                if (p.playerID == this.playerID.value)
                {
                    kills.value = p.GetPlayerInfoSO().KillCount;
                    deaths.value = p.GetPlayerInfoSO().DeathCount;
                }
            }
        }

        if (avatarImage.sprite == null || avatar == null)
        {
            setupAvatar();
        }

        displayNameText.text = displayName.value;
        switch(character.value.ToLower())
        {
            case "mosquito":
                characterIcon.sprite = characterIcons[0];
                break;
            case "beetle":
                characterIcon.sprite = characterIcons[1];
                break;
            case "butterfly":
                characterIcon.sprite = characterIcons[2];
                break;
            default:
                characterIcon.sprite = null;
                break;
        }

        Color teamColor;
        switch(team.value)
        {
            case 1:
                teamColor = GameManager.Instance.team1Color; 
                break;
            case 2:
                teamColor = GameManager.Instance.team2Color;
                break;
            default:
                teamColor = Color.gray;
                break;
        }
        teamColor.a = 0.5f;
        backgroundImage.color = teamColor;

        killsText.text = $"Kills: {kills}";
        deathsText.text = $"Deaths: {deaths}";
    }

    public float GetHeight()
    {
        return backgroundImage.rectTransform.rect.height;
    }

    public void setupAvatar()
    {
        LobbyPlayerRegistry lobbyPlayerRegistry = FindAnyObjectByType<LobbyPlayerRegistry>();
        List<GameManager.PlayerInfo> playerInfo = GameManager.Instance.GetPlayerInfos();

        if (lobbyPlayerRegistry != null)
        {
            List<LobbyUser> lobbyUsers = lobbyPlayerRegistry.GetPlayers();
            foreach (LobbyUser lobbyUser in lobbyUsers)
            {
                if (lobbyUser.Id == steamID.value)
                {
                    avatar = lobbyUser.Avatar;
                    avatarImage.sprite = Sprite.Create(avatar, new Rect(0, 0, avatar.width, avatar.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
    }
}
