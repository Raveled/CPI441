// ******************************************* //
// ****** THEO XENAKIS - 2026 - CPI 441 ****** //
// ******************************************* //

using UnityEngine;
[RequireComponent(typeof(Rigidbody))]

public class PlayerRotate : MonoBehaviour
{
    // ******************************************* //
    // *** Initializations *** //

    private Camera mainCamera;
    private Rigidbody rb;
    public float rotationSpeed = 10f;

    // ****************************************** //
    // *** Monobehavior Functions *** //

    private void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();

        // Disable physics rotation control
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }
    private void Update()
    {
        if (mainCamera != null)
        {
            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0f;

            // Safety check to ensure no crashing when at world origina * camera forward is perfectly zero!
            if (cameraForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
}
// ******************************************** //