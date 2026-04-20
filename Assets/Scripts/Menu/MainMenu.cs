using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void QuitGame() {
        Application.Quit();
    }
    public void ToggleSettingsMenu(bool open) {
        if (open) {
            //open settings
        } else {
            //close settings
        }
    }
}
