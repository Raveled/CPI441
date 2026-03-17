using UnityEngine;

public class DazzlingWaveProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 5f;  // Tune movement speed

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
