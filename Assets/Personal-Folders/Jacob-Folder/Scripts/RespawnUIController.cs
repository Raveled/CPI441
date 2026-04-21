using UnityEngine;
using UnityEngine.UI;

public class RespawnUIController : MonoBehaviour
{
    public Image respawnBackground;
    public TMPro.TMP_Text respawnText;
   
    void Awake()
    {
        Hide();
    }

    public void Hide()
    {
        Debug.Log("Hiding Respawn UI");
        respawnText.alpha = 0f;
        respawnBackground.color = new Color(respawnBackground.color.r, respawnBackground.color.g, respawnBackground.color.b, 0f);
    }

    public void Show()
    {
        Debug.Log("Showing Respawn UI");
        respawnText.alpha = 1f;
        respawnBackground.color = new Color(respawnBackground.color.r, respawnBackground.color.g, respawnBackground.color.b, 1f);
    }
}