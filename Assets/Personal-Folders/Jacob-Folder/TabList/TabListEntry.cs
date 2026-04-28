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
    [SerializeField] private List<Sprite> characterIcons;

    public string steamID;
    public PlayerID playerID;
    public string displayName;
    public Texture2D avatar;
    public int team;
    public string character;

    private Player player;

    public void Init(string steamID, PlayerID playerID, string displayName, Texture2D avatar, int team, string character)
    {
        this.steamID = steamID;
        this.playerID = playerID;
        this.displayName = displayName;
        this.avatar = avatar;
        this.team = team;
        this.character = character;

        foreach (Player player in FindObjectsByType<Player>(FindObjectsSortMode.None))
        {
            if (player.GetPlayerID() == playerID)
            {
                this.player = player;
                break; 
            }
        }

        displayNameText.text = displayName;
        avatarImage.sprite = avatar != null ? Sprite.Create(avatar, new Rect(0, 0, avatar.width, avatar.height), new Vector2(0.5f, 0.5f)) : null;
        switch(character.ToLower())
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
        switch(team)
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
    }

    public float GetHeight()
    {
        return backgroundImage.rectTransform.rect.height;
    }

    public void UpdateEntry()
    {
        
    }
}
