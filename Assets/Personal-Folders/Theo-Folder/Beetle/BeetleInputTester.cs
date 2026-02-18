// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeetleInputTester : MonoBehaviour
{
    private Beetle beetle;

    [Header("Input Actions")]
    public InputActionAsset actions;

    public string mandibleAttackAction = "MandibleAttack";
    public string hornImpaleAction = "HornImpale";
    public string swaggerAction = "Swagger";
    public string rollAction = "Roll";
    public string groundStompAction = "GroundStomp";

    private InputAction mandibleAttack;
    private InputAction hornImpale;
    private InputAction swagger;
    private InputAction roll;
    private InputAction groundStomp;

    void Awake()
    {
        beetle = GetComponent<Beetle>();

        // Resolve actions by name (Unity 6 safe)
        mandibleAttack = actions.FindAction(mandibleAttackAction, throwIfNotFound: true);
        hornImpale = actions.FindAction(hornImpaleAction, throwIfNotFound: true);
        swagger = actions.FindAction(swaggerAction, throwIfNotFound: true);
        roll = actions.FindAction(rollAction, throwIfNotFound: true);
        groundStomp = actions.FindAction(groundStompAction, throwIfNotFound: true);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Input Callbacks
    private void OnMandibleAttack(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null)
        {
            beetle.MandibleAttack();
            Debug.Log("Mandible Attack!");
        }
    }

    private void OnHornImpale(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null)
        {
            beetle.HornImpale();
            Debug.Log("Horn Impale!");
        }
    }

    private void OnSwagger(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null)
        {
            beetle.ActivateSwagger();
            Debug.Log("Swagger!");
        }
    }

    private void OnRoll(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null)
        {
            beetle.StartRoll();
            Debug.Log("Roll!");
        }
    }

    private void OnGroundStomp(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null)
        {
            beetle.GroundStomp();
            Debug.Log("Ground Stomp!");
        }
    }

    // Enable / Disable
    private void OnEnable()
    {
        mandibleAttack.started += OnMandibleAttack;
        hornImpale.started += OnHornImpale;
        swagger.started += OnSwagger;
        roll.started += OnRoll;
        groundStomp.started += OnGroundStomp;

        mandibleAttack.Enable();
        hornImpale.Enable();
        swagger.Enable();
        roll.Enable();
        groundStomp.Enable();
    }

    private void OnDisable()
    {
        mandibleAttack.started -= OnMandibleAttack;
        hornImpale.started -= OnHornImpale;
        swagger.started -= OnSwagger;
        roll.started -= OnRoll;
        groundStomp.started -= OnGroundStomp;

        mandibleAttack.Disable();
        hornImpale.Disable();
        swagger.Disable();
        roll.Disable();
        groundStomp.Disable();
    }
}