// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TempInputTester : MonoBehaviour
{
    // **************************************************** //
    // *** Variable Initializations *** //
    private Mosquito mosquito;
    private Butterfly butterfly;

    public InputActionReference quickPoke;
    public InputActionReference globShot;
    public InputActionReference ampUp;
    public InputActionReference basicAttack;

    // Butterfly inputs
    public InputActionReference dustWave;
    public InputActionReference dazzlingWave;
    public InputActionReference flyDash;
    public InputActionReference tornado;

    // **************************************************** //
    // *** Monobehavior Functions *** //
    void Awake()
    {
        mosquito = GetComponent<Mosquito>();
        butterfly = GetComponent<Butterfly>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // **************************************************** //
    // *** Mosquito Input Actions *** //
    private void OnQuickPoke(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Entity target = FindNearestEnemy();
            if (mosquito != null && mosquito.TryQuickPoke())
                Debug.Log("Quick Poke HIT!");
        }
    }

    private void OnGlobShot(InputAction.CallbackContext context)
    {
        if (context.started && mosquito != null)
        {
            mosquito.CastGlobShot();
            Debug.Log("Glob Shot FIRED!");
        }
    }

    private void OnAmpUp(InputAction.CallbackContext context)
    {
        if (context.started && mosquito != null)
        {
            mosquito.ActivateAmpUp();
            Debug.Log("Amp Up ACTIVATED!");
        }
    }

    private void OnBasicAttack(InputAction.CallbackContext context)
    {
        if (context.started && mosquito != null)
        {
            Entity target = FindNearestEnemy();
            if (target != null && mosquito.entity != null)
            {
                mosquito.CastBloodShot();
                Debug.Log("Blood Shot FIRED!");
            }
        }
    }

    // **************************************************** //
    // *** Butterfly Input Actions *** //
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
            Vector3 flyDirection = transform.forward; // Forward dash, modify as needed
            butterfly.StartFly(flyDirection);
            Debug.Log("Fly DASH!");
        }
    }

    private void OnTornado(InputAction.CallbackContext context)
    {
        if (context.started && butterfly != null)
        {
            // Aim at mouse position or center screen for tornado
            Vector3 tornadoPos = transform.position + transform.forward * 5f;
            butterfly.CastTornado(tornadoPos);
            Debug.Log("Tornado SUMMONED!");
        }
    }

    // **************************************************** //
    // *** Helper Functions *** //
    Entity FindNearestEnemy()
    {
        Entity owner = mosquito?.entity ?? butterfly?.entity;
        if (owner == null) return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        Entity nearest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy.GetTeam() != owner.GetTeam())
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = enemy;
                }
            }
        }
        return nearest;
    }

    // **************************************************** //
    // *** Handle Enabling and Disabling Actions *** //
    private void OnEnable()
    {
        quickPoke.action.started += OnQuickPoke;
        globShot.action.started += OnGlobShot;
        ampUp.action.started += OnAmpUp;
        basicAttack.action.started += OnBasicAttack;

        // Butterfly actions
        dustWave.action.started += OnDustWave;
        dazzlingWave.action.started += OnDazzlingWave;
        flyDash.action.started += OnFlyDash;
        tornado.action.started += OnTornado;
    }

    private void OnDisable()
    {
        quickPoke.action.started -= OnQuickPoke;
        globShot.action.started -= OnGlobShot;
        ampUp.action.started -= OnAmpUp;
        basicAttack.action.started -= OnBasicAttack;

        // Butterfly actions
        dustWave.action.started -= OnDustWave;
        dazzlingWave.action.started -= OnDazzlingWave;
        flyDash.action.started -= OnFlyDash;
        tornado.action.started -= OnTornado;
    }
    // ******************************************* //
}
