// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BeetleInputTester : MonoBehaviour
{
    private Beetle beetle;

    public InputActionReference mandibleAttack;
    public InputActionReference hornImpale;
    public InputActionReference swagger;
    public InputActionReference roll;
    public InputActionReference groundStomp;

    void Awake()
    {
        beetle = GetComponent<Beetle>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

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

    private void OnEnable()
    {
        mandibleAttack.action.started += OnMandibleAttack;
        hornImpale.action.started += OnHornImpale;
        swagger.action.started += OnSwagger;
        roll.action.started += OnRoll;
        groundStomp.action.started += OnGroundStomp;
    }

    private void OnDisable()
    {
        mandibleAttack.action.started -= OnMandibleAttack;
        hornImpale.action.started -= OnHornImpale;
        swagger.action.started -= OnSwagger;
        roll.action.started -= OnRoll;
        groundStomp.action.started -= OnGroundStomp;
    }
}
