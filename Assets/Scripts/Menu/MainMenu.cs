using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject settingsMenu;
    [SerializeField] GameObject creditsMenu;
    [SerializeField] GameObject characterInfoMenu;
    private void Start() {
        if(settingsMenu)settingsMenu.SetActive(false);
        if(creditsMenu)creditsMenu.SetActive(false);
        if(characterInfoMenu)characterInfoMenu.SetActive(false);
        if (FindFirstObjectByType<GameManager>())
        {

            Destroy(FindFirstObjectByType<GameManager>());
        }
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
    public void ToggleCharacterInfoMenu(bool open) {
        HelperToggleMenu(characterInfoMenu, open);

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
