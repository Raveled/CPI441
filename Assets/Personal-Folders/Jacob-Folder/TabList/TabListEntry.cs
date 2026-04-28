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
    public SyncVar<Texture2D> avatar;
    public SyncVar<int> team;
    public SyncVar<string> character;
    public SyncVar<int> kills;
    public SyncVar<int> deaths;

    public void Init(string steamID, PlayerID playerID, string displayName, Texture2D avatar, int team, string character)
    {
        this.steamID.value = steamID;
        this.playerID.value = playerID;
        this.displayName.value = displayName;
        this.avatar.value = avatar;
        this.team.value = team;
        this.character.value = character;
        kills.value = 0;
        deaths.value = 0;
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

        displayNameText.text = displayName.value;
        avatarImage.sprite = avatar.value != null ? Sprite.Create(avatar.value, new Rect(0, 0, avatar.value.width, avatar.value.height), new Vector2(0.5f, 0.5f)) : null;
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
}
