using UnityEngine;

public class DisableVisualOnStart : MonoBehaviour
{
    //Disables Mesh Renderer on start
    void Start()
    {
        Renderer m = GetComponent<Renderer>();
        m.enabled = false;
    }
}
