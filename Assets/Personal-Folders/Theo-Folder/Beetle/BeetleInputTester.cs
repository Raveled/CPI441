// BeetleInputTester.cs - FULLY ADAPTED TO MOSQUITO INPUT TESTER STRUCTURE
// ******************************************* 
// ****** THEO XENAKIS - 2026 - CPI 441 ****** 
// ******************************************* 

using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeetleInputTester : MonoBehaviour
{
    private Beetle beetle;

    [Header("Input Actions")]
    public InputActionAsset actions;

    public string mandibleAction = "BasicAttack";
    public string hornImpaleAction = "QuickPoke";
    public string swaggerAction = "GlobShot";
    public string rollAction = "AmpUp";
    public string groundStompAction = "Ultimate";

    private InputAction mandible;
    private InputAction hornImpale;
    private InputAction swagger;
    private InputAction roll;
    private InputAction groundStomp;

    void Awake()
    {
        beetle = GetComponent<Beetle>();

        // Resolve actions by name (Unity 6 safe)
        mandible = actions.FindAction(mandibleAction, throwIfNotFound: true);
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
        if (context.started && beetle != null && beetle.isOwner)
        {
            beetle.CastMandibleAttack();
            Debug.Log("[InputTester] Mandible Attack Tried!");
        }
    }

    private void OnHornImpale(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null && beetle.isOwner)
        {
            if (beetle.TryHornImpale())
                Debug.Log("[InputTester] Horn Impale Tried!");
        }
    }

    private void OnSwagger(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null && beetle.isOwner)
        {
            beetle.ActivateSwagger();
            Debug.Log("[InputTester] Swagger Tried!");
        }
    }

    private void OnRoll(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null && beetle.isOwner)
        {
            if (beetle.TryRoll())
                Debug.Log("[InputTester] Roll Tried!");
        }
    }

    private void OnGroundStomp(InputAction.CallbackContext context)
    {
        if (context.started && beetle != null && beetle.isOwner)
        {
            beetle.CastGroundStomp();
            Debug.Log("[InputTester] Ground Stomp Tried!");
        }
    }

    // Enemy Search (same as Mosquito)
    Entity FindNearestEnemy()
    {
        if (beetle?.entity == null) return null;

        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        Entity nearest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy.GetTeam() != beetle.entity.GetTeam())
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

    private bool _inputEnabled = false;

    // Called by Beetle.OnSpawned — only subscribes input for the local owner
    public void EnableInput()
    {
        if (_inputEnabled) return;
        _inputEnabled = true;

        Debug.Log($"[InputTester] EnableInput called on {beetle.gameObject.name} | owner={beetle.owner}");

        mandible.started += OnMandibleAttack;
        hornImpale.started += OnHornImpale;
        swagger.started += OnSwagger;
        roll.started += OnRoll;
        groundStomp.started += OnGroundStomp;

        mandible.Enable();
        hornImpale.Enable();
        swagger.Enable();
        roll.Enable();
        groundStomp.Enable();
    }

    private void OnDisable()
    {
        mandible.started -= OnMandibleAttack;
        hornImpale.started -= OnHornImpale;
        swagger.started -= OnSwagger;
        roll.started -= OnRoll;
        groundStomp.started -= OnGroundStomp;

        mandible.Disable();
        hornImpale.Disable();
        swagger.Disable();
        roll.Disable();
        groundStomp.Disable();
    }
}
