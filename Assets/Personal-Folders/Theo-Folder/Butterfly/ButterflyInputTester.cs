// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ButterflyInputTester : MonoBehaviour
{
    // Variable Initializations
    private Butterfly butterfly;

    // ADD THIS LINE - Basic Attack input
    public InputActionReference basicAttack;
    public InputActionReference dustWave;
    public InputActionReference dazzlingWave;
    public InputActionReference flyDash;
    public InputActionReference tornado;

    void Awake()
    {
        butterfly = GetComponent<Butterfly>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ADD THIS METHOD - Basic Attack (Wind Burst)
    private void OnBasicAttack(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            butterfly.CastWindBurst();
            Debug.Log("Wind Burst FIRED!");
        }
    }

    // Input Actions
    private void OnDustWave(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            butterfly.CastDustWave();
            Debug.Log("Dust Wave CAST!");
        }
    }

    private void OnDazzlingWave(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            butterfly.CastDazzlingWave();
            Debug.Log("Dazzling Wave CAST!");
        }
    }

    private void OnFlyDash(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            Vector3 flyDirection = transform.forward;
            butterfly.StartFly(flyDirection);
            Debug.Log("Fly DASH!");
        }
    }

    private void OnTornado(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            Vector3 tornadoPos = transform.position + transform.forward * 5f;
            butterfly.CastTornado(tornadoPos);
            Debug.Log("Tornado SUMMONED!");
        }
    }

    // Handle Enabling and Disabling Actions
    private void OnEnable()
    {
        // ADD THIS LINE
        basicAttack.action.started += OnBasicAttack;
        dustWave.action.started += OnDustWave;
        dazzlingWave.action.started += OnDazzlingWave;
        flyDash.action.started += OnFlyDash;
        tornado.action.started += OnTornado;
    }

    private void OnDisable()
    {
        // ADD THIS LINE
        basicAttack.action.started -= OnBasicAttack;
        dustWave.action.started -= OnDustWave;
        dazzlingWave.action.started -= OnDazzlingWave;
        flyDash.action.started -= OnFlyDash;
        tornado.action.started -= OnTornado;
    }
}
