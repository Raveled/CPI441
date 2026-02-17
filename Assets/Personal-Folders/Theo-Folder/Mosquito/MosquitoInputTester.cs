// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MosquitoInputTester : MonoBehaviour
{
    private Mosquito mosquito;

    [Header("Input Actions")]
    public InputActionAsset actions;

    public string quickPokeAction = "QuickPoke";
    public string globShotAction = "GlobShot";
    public string ampUpAction = "AmpUp";
    public string basicAttackAction = "BasicAttack";

    private InputAction quickPoke;
    private InputAction globShot;
    private InputAction ampUp;
    private InputAction basicAttack;

    void Awake()
    {
        mosquito = GetComponent<Mosquito>();

        // Resolve actions by name (Unity 6 safe)
        quickPoke = actions.FindAction(quickPokeAction, throwIfNotFound: true);
        globShot = actions.FindAction(globShotAction, throwIfNotFound: true);
        ampUp = actions.FindAction(ampUpAction, throwIfNotFound: true);
        basicAttack = actions.FindAction(basicAttackAction, throwIfNotFound: true);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Input Callbacks
    private void OnQuickPoke(InputAction.CallbackContext context)
    {
        if (context.started && mosquito != null)
        {
            Entity target = FindNearestEnemy();
            if (mosquito.TryQuickPoke(target))
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
            mosquito.CastBloodShot();
            Debug.Log("Blood Shot FIRED!");
        }
    }

    // Enemy Search
    Entity FindNearestEnemy()
    {
        if (mosquito?.entity == null) return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        Entity nearest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy.GetTeam() != mosquito.entity.GetTeam())
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

    // Enable / Disable
    private void OnEnable()
    {
        quickPoke.started += OnQuickPoke;
        globShot.started += OnGlobShot;
        ampUp.started += OnAmpUp;
        basicAttack.started += OnBasicAttack;

        quickPoke.Enable();
        globShot.Enable();
        ampUp.Enable();
        basicAttack.Enable();
    }

    private void OnDisable()
    {
        quickPoke.started -= OnQuickPoke;
        globShot.started -= OnGlobShot;
        ampUp.started -= OnAmpUp;
        basicAttack.started -= OnBasicAttack;

        quickPoke.Disable();
        globShot.Disable();
        ampUp.Disable();
        basicAttack.Disable();
    }
}