// TempInputTester.cs - Attach to MosquitoPlayer
using UnityEngine;

public class TempInputTester : MonoBehaviour
{
    private Mosquito mosquito;
    private Camera cam;

    private void Awake()
    {
        mosquito = GetComponent<Mosquito>();
        cam = Camera.main;
    }

    private void Update()
    {
        // Q = Quick Poke nearest enemy
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Entity target = FindNearestEnemy();
            if (mosquito.TryQuickPoke(target))
                Debug.Log("Quick Poke HIT!");
        }

        // E = Glob Shot
        if (Input.GetKeyDown(KeyCode.E))
        {
            mosquito.CastGlobShot();
            Debug.Log("Glob Shot fired!");
        }

        // R = Amp Up
        if (Input.GetKeyDown(KeyCode.R))
        {
            mosquito.ActivateAmpUp();
            Debug.Log("Amp Up ACTIVATED!");
        }

        // Mouse1 = Basic attack (for blood gain testing)
        if (Input.GetMouseButtonDown(0))
        {
            Entity target = FindNearestEnemy();
            if (target != null)
            {
                // Simulate basic attack
                int damage = mosquito.GetBasicAttackDamageWithBlood(mosquito.entity.attackPower);
                target.TakeDamage(damage, mosquito.entity);
                mosquito.OnBasicAttackHit(target);
                Debug.Log($"Basic attack: {damage} dmg, blood: {mosquito.GetBloodMeter01():F1}");
            }
        }
    }

    Entity FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        Entity nearest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && enemy.GetTeam() != mosquito.entity.GetTeam())
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = enemy;
                }
            }
        }
        return nearest;
    }
}
