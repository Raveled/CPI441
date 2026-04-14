using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;

    private void Awake()
    {
        Instance = this;
        winScreen.SetActive(false);
        loseScreen.SetActive(false);
    }

    public void ShowEndScreen(bool didWin)
    {
        if (didWin)
            winScreen.SetActive(true);
        else
            loseScreen.SetActive(true);
    }
}
