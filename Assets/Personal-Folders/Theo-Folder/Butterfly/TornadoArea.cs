// TornadoArea.cs - full rewrite
using UnityEngine;
using System.Collections;

public class TornadoArea : MonoBehaviour
{
    [HideInInspector] public Entity ownerEntity;
    [HideInInspector] public float radius = 5f;
    [HideInInspector] public float duration = 5f;
    [HideInInspector] public int damagePerTick = 2;
    [HideInInspector] public float tickInterval = 0.5f;
    [HideInInspector] public float groupForce = 10f;
    [HideInInspector] public Vector3 travelDirection = Vector3.forward;

    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 4f;   // units/sec moving forward
    [SerializeField] private float spiralRadius = 1.5f; // how wide the spiral sweeps
    [SerializeField] private float spiralSpeed = 3f;    // radians/sec of spiral rotation

    private float timer;
    private float spiralAngle;
    private bool initialized;

    // Called explicitly by Butterfly after setting all fields
    // TornadoArea.cs - Init and Update with verbose debug
    private Vector3 basePosition; // tracks pure forward movement

    public void Init()
    {
        if (initialized) return;
        initialized = true;
        timer = duration;
        spiralAngle = 0f;
        basePosition = transform.position;
        StartCoroutine(DamageRoutine());
    }

    private void Update()
    {
        if (!initialized) return;

        // Advance base position forward
        basePosition += travelDirection * forwardSpeed * Time.deltaTime;

        // Spiral wobble around the base
        spiralAngle += spiralSpeed * Time.deltaTime;
        Vector3 right = Vector3.Cross(travelDirection, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, travelDirection).normalized;
        Vector3 spiralOffset = (right * Mathf.Cos(spiralAngle) + up * Mathf.Sin(spiralAngle)) * spiralRadius;

        transform.position = basePosition + spiralOffset;
    }

    private IEnumerator DamageRoutine()
    {
        Debug.Log("[TornadoArea] DamageRoutine started");
        while (timer > 0f)
        {
            timer -= tickInterval;
            Debug.Log($"[TornadoArea] Tick — timer={timer}, checking overlaps at {transform.position} radius={radius}");

            Collider[] hits = Physics.OverlapSphere(transform.position, radius);
            Debug.Log($"[TornadoArea] OverlapSphere hit {hits.Length} colliders");

            foreach (var hit in hits)
            {
                Entity target = hit.GetComponent<Entity>();
                if (target == null || ownerEntity == null) continue;
                if (target == ownerEntity || target.GetTeam() == ownerEntity.GetTeam()) continue;

                target.TakeDamage(damagePerTick, ownerEntity);

                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 dirToCenter = (transform.position - target.transform.position).normalized;
                    rb.AddForce(dirToCenter * groupForce, ForceMode.Acceleration);
                }
            }

            yield return new WaitForSeconds(tickInterval);
        }

        Debug.Log("[TornadoArea] DamageRoutine finished, destroying");
        Destroy(gameObject);
        yield break;
    }
}