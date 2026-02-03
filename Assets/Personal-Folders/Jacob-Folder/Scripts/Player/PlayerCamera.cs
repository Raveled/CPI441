using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] public float lookSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    private Vector2 currentLookRotation;
    public bool initialized = false;

    public Vector3 forward => Quaternion.Euler(currentLookRotation.x, currentLookRotation.y, 0f) * Vector3.forward;

    public Vector2 lookInput;
    public InputAction lookAction;

    private void Awake()
    {
        playerCamera.Priority.Value = -1;
    }

    public void Init()
    {
        initialized = true;
        playerCamera.Priority.Value = 10;

        lookAction = InputSystem.actions.FindAction("Look");

        lookAction?.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        lookInput = lookAction.ReadValue<Vector2>();

        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

        currentLookRotation.x = Mathf.Clamp(currentLookRotation.x - mouseY, -maxLookAngle, maxLookAngle);
        currentLookRotation.y += mouseX;

        transform.localRotation = Quaternion.Euler(currentLookRotation.x, 0f, 0f);
    }
}