// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class KeybindRebinder : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset actions;

    public string quickPokeAction = "QuickPoke";
    public string globShotAction = "GlobShot";
    public string ampUpAction = "AmpUp";
    public string basicAttackAction = "BasicAttack";

    [Header("Rebind Buttons (TMP)")]
    public TMP_Text quickPokeLabel;
    public TMP_Text globShotLabel;
    public TMP_Text ampUpLabel;
    public TMP_Text basicAttackLabel;

    public GameObject rebindOverlay; // "Press any key..." overlay

    private InputAction _quickPoke;
    private InputAction _globShot;
    private InputAction _ampUp;
    private InputAction _basicAttack;

    private InputActionRebindingExtensions.RebindingOperation _currentRebind;

    void Awake()
    {
        _quickPoke = actions.FindAction(quickPokeAction, throwIfNotFound: true);
        _globShot = actions.FindAction(globShotAction, throwIfNotFound: true);
        _ampUp = actions.FindAction(ampUpAction, throwIfNotFound: true);
        _basicAttack = actions.FindAction(basicAttackAction, throwIfNotFound: true);
    }

    void OnEnable()
    {
        RefreshAllLabels();
    }

    // Called by each rebind button in the Inspector
    public void StartRebind_QuickPoke() => StartRebind(_quickPoke, quickPokeLabel);
    public void StartRebind_GlobShot() => StartRebind(_globShot, globShotLabel);
    public void StartRebind_AmpUp() => StartRebind(_ampUp, ampUpLabel);
    public void StartRebind_BasicAttack() => StartRebind(_basicAttack, basicAttackLabel);

    private void StartRebind(InputAction action, TMP_Text label)
    {
        action.Disable();
        rebindOverlay.SetActive(true);

        _currentRebind = action.PerformInteractiveRebinding(0)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                FinishRebind(action, label);
            })
            .OnCancel(op =>
            {
                FinishRebind(action, label);
            })
            .Start();
    }

    private void FinishRebind(InputAction action, TMP_Text label)
    {
        _currentRebind?.Dispose();
        _currentRebind = null;

        action.Enable();
        rebindOverlay.SetActive(false);
        RefreshLabel(action, label);
    }

    private void RefreshAllLabels()
    {
        RefreshLabel(_quickPoke, quickPokeLabel);
        RefreshLabel(_globShot, globShotLabel);
        RefreshLabel(_ampUp, ampUpLabel);
        RefreshLabel(_basicAttack, basicAttackLabel);
    }

    private void RefreshLabel(InputAction action, TMP_Text label)
    {
        int bindingIndex = action.GetBindingIndexForControl(action.controls[0]);
        label.text = InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }
}