// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MosquitoInputTester : MonoBehaviour
{
    // Variable Initializations
    private Mosquito mosquito;

    public InputActionReference quickPoke;
    public InputActionReference globShot;
    public InputActionReference ampUp;
    public InputActionReference basicAttack;

    void Awake()
    {
        mosquito = GetComponent<Mosquito>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Input Actions
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

    // Helper Functions
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

    // Handle Enabling and Disabling Actions
    private void OnEnable()
    {
        quickPoke.action.started += OnQuickPoke;
        globShot.action.started += OnGlobShot;
        ampUp.action.started += OnAmpUp;
        basicAttack.action.started += OnBasicAttack;
    }

    private void OnDisable()
    {
        quickPoke.action.started -= OnQuickPoke;
        globShot.action.started -= OnGlobShot;
        ampUp.action.started -= OnAmpUp;
        basicAttack.action.started -= OnBasicAttack;
    }
}
