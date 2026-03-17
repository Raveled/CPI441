using PurrNet;
using UnityEngine;

public class MinimapTracker : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private string minimapCameraName = "MinimapCamera";
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 20f, 0f);
    [SerializeField] private bool lockRotation = false;

    private Transform minimapCameraTransform;

    public void AttachMinimapCamera()
    {
        Debug.Log($"[MinimapTracker] AttachMinimapCamera called on '{gameObject.name}'");
        
        // Find the camera by name anywhere in the scene
        GameObject minimapCameraObject = GameObject.Find(minimapCameraName);

        if (minimapCameraObject == null)
        {
            Debug.LogWarning($"[MinimapCameraFollow] Could not find a GameObject named '{minimapCameraName}' in the scene.");
            return;
        }

        minimapCameraTransform = minimapCameraObject.transform;
        minimapCameraTransform.SetParent(transform);
        minimapCameraTransform.localPosition = localOffset;
        minimapCameraTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Debug.Log($"[MinimapCameraFollow] '{minimapCameraName}' successfully attached to '{gameObject.name}'.");
    }

    private void LateUpdate()
    {
        if (lockRotation && minimapCameraTransform != null)
        {
            minimapCameraTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
