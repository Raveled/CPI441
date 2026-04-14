using UnityEngine;

public class RespawnUIController : MonoBehaviour
{
    public static RespawnUIController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}