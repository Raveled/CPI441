// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ButterflyInputTester : MonoBehaviour
{
    private Butterfly butterfly;

    [Header("Input Actions")]
    public InputActionAsset actions;

    public string basicAttackAction = "BasicAttack";
    public string dustWaveAction = "DustWave";
    public string dazzlingWaveAction = "DazzlingWave";
    public string flyDashAction = "FlyDash";
    public string tornadoAction = "Tornado";

    private InputAction basicAttack;
    private InputAction dustWave;
    private InputAction dazzlingWave;
    private InputAction flyDash;
    private InputAction tornado;

    void Awake()
    {
        butterfly = GetComponent<Butterfly>();

        // Resolve actions by name (Unity 6 safe)
        basicAttack = actions.FindAction(basicAttackAction, throwIfNotFound: true);
        dustWave = actions.FindAction(dustWaveAction, throwIfNotFound: true);
        dazzlingWave = actions.FindAction(dazzlingWaveAction, throwIfNotFound: true);
        flyDash = actions.FindAction(flyDashAction, throwIfNotFound: true);
        tornado = actions.FindAction(tornadoAction, throwIfNotFound: true);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Input Callbacks
    private void OnBasicAttack(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            butterfly.CastWindBurst();
            Debug.Log("Wind Burst FIRED!");
        }
    }

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

    // Enable / Disable
    private void OnEnable()
    {
        basicAttack.started += OnBasicAttack;
        dustWave.started += OnDustWave;
        dazzlingWave.started += OnDazzlingWave;
        flyDash.started += OnFlyDash;
        tornado.started += OnTornado;

        basicAttack.Enable();
        dustWave.Enable();
        dazzlingWave.Enable();
        flyDash.Enable();
        tornado.Enable();
    }

    private void OnDisable()
    {
        basicAttack.started -= OnBasicAttack;
        dustWave.started -= OnDustWave;
        dazzlingWave.started -= OnDazzlingWave;
        flyDash.started -= OnFlyDash;
        tornado.started -= OnTornado;

        basicAttack.Disable();
        dustWave.Disable();
        dazzlingWave.Disable();
        flyDash.Disable();
        tornado.Disable();
    }
}