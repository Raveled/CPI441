using UnityEngine;

public class DustWaveMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}