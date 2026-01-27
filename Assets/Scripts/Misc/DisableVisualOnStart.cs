using UnityEngine;

public class DisableVisualOnStart : MonoBehaviour
{
    void Awake()
    {
        //Disables Mesh Renderer on Awake
        Renderer m = GetComponent<Renderer>();
        m.enabled = false;
    }
}
