using System;
using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = System.Object;

[RequireComponent(typeof(PlayerInput))]
public class PlayerMovementNetworked : NetworkIdentity
{
    [SerializeField] private float _speed = 3f;
    public Vector3 movementInputVector;
    private PlayerInput _playerInput;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();

        // HARD DISABLE input immediately
        _playerInput.enabled = false;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();

        // Enable input ONLY for owning client
        if (isOwner)
        {
            _playerInput.enabled = true;
        }
    }

    private void OnMove(InputValue inputValue)
    {
        movementInputVector = new Vector3(inputValue.Get<Vector2>().x, 0f, inputValue.Get<Vector2>().y);
    }

    private void Update()
    {
        if (!isOwner) return;
        
        transform.position += movementInputVector * (_speed * Time.deltaTime);
    }
}
