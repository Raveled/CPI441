using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject creditsMenu;
    private void Start() {
        settingsMenu.SetActive(false);
        creditsMenu.SetActive(false);
    }
    public void QuitGame() {
        Application.Quit();
    }
    public void ToggleSettingsMenu(bool open) {
        HelperToggleMenu(settingsMenu, open);
    }
    public void ToggleCreditsMenu(bool open) {
        HelperToggleMenu(creditsMenu, open);
    }
    void HelperToggleMenu(GameObject menu, bool open) {
        if (open) {
            menu.SetActive(true);
        }
        else{
            menu.SetActive(false);
        }
    }
}
