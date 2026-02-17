using UnityEngine;

public class DisableVisualOnStart : MonoBehaviour
{
    [SerializeField] bool disableOnAwake = true;
    void Awake()
    {
        //Disables Mesh Renderer on Awake
        Renderer m = GetComponent<Renderer>();
        if(disableOnAwake) m.enabled = false;
    }
}
