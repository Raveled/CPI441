// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset actions;
    public string pauseMenuAction = "PauseMenu";

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject optionsPanel;

    [Header("Options - Audio")]
    public Slider volumeSlider;

    [Header("Buttons - Pause")]
    public Button resumeButton;
    public Button optionsButton;
    public Button leaveButton;

    [Header("Buttons - Options")]
    public Button optionsBackButton;

    private InputAction _pauseAction;
    private bool _isPauseOpen = false;
    private bool _isOptionsOpen = false;

    void Awake()
    {
        _pauseAction = actions.FindAction(pauseMenuAction, throwIfNotFound: true);
        _pauseAction.performed += _ => TogglePause();
    }

    void Start()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);

        resumeButton.onClick.AddListener(ClosePause);
        optionsButton.onClick.AddListener(OpenOptions);
        leaveButton.onClick.AddListener(LeaveGame);
        optionsBackButton.onClick.AddListener(CloseOptions);

        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(val => AudioListener.volume = val);
    }

    void OnEnable()
    {
        _pauseAction.Enable();
    }

    void OnDisable()
    {
        _pauseAction.performed -= _ => TogglePause();
        _pauseAction.Disable();
    }

    public void TogglePause()
    {
        if (_isOptionsOpen)
        {
            CloseOptions();
            return;
        }

        if (_isPauseOpen)
            ClosePause();
        else
            OpenPause();
    }

    public void OpenPause()
    {
        _isPauseOpen = true;
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ClosePause()
    {
        _isPauseOpen = false;
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenOptions()
    {
        _isOptionsOpen = true;
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        _isOptionsOpen = false;
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void LeaveGame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Lobby");
    }
}