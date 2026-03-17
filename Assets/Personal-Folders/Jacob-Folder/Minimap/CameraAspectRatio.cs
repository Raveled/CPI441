using UnityEngine;

public class CameraAspectRatio : MonoBehaviour
{
    public Camera targetCamera;
    [SerializeField] public float targetAspect = 8f / 10f;

    void Start()
    {
        if (targetCamera != null)
        {
            targetCamera.aspect = targetAspect;
        }
    }

    public void ChangeAspect(float width, float height)
    {
        if (targetCamera != null)
        {
            targetCamera.aspect = width / height;
        }
    }

    public void ResetAspect()
    {
        if (targetCamera != null)
        {
            targetCamera.ResetAspect();
        }
    }
}
