using UnityEngine;

public class CreepSpawner : MonoBehaviour
{
    [Header("Creep Spawner Setup")]
    [SerializeField] Transform patrolOrigin = null;
    [SerializeField] GameObject[] spawnpoints = null;
    [SerializeField] float maxCampTimer = 10f;
    [Space]
    [Header("Creep Spawner Debug")]
    [SerializeField] bool disableVisualOnStart = true;
    [Space]
    [SerializeField] int activeCreepCount = 0;
    [SerializeField] float currentCampTimer = 0f;
    private void Start() {
        //Disable the visual (used for editor)
        if (disableVisualOnStart) {
            TryGetComponent<MeshRenderer>(out MeshRenderer m);
            m.enabled = false;
            patrolOrigin.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer n);
            n.enabled = false;
        }

        //Init
        SpawnCreeps();
        currentCampTimer = maxCampTimer;
    }
    private void Update() {
        CampTimer();
    }
    //Cooldown timer for spawning
    void CampTimer() {
        //If camp can spawn, it will
        if (currentCampTimer <= 0) {
            SpawnCreeps();
            currentCampTimer = maxCampTimer;
        }

        //Timer only goes down once there are no remaining creeps in a camp
        if (activeCreepCount <= 0) {
            currentCampTimer -= Time.deltaTime;
        }
    }
    //Spawn a creep on each connected spawnpoint of that spawnpoint's type
    void SpawnCreeps() {
        //For each spawnpoint, instantiate a creep of it's creepType field
        for (int i = 0; i < spawnpoints.Length; i++) {
            if (spawnpoints[i].TryGetComponent<CreepSpawnpoint>(out CreepSpawnpoint spawnpoint)) {
                GameObject creep = Instantiate(spawnpoint.GetCreepType(), spawnpoints[i].transform.position, spawnpoints[i].transform.rotation, spawnpoints[i].transform);
                creep.TryGetComponent<Creep>(out Creep c);
                c.SetConnectedSpawner(this);
                c.SetPatrolOrigin(patrolOrigin);
                activeCreepCount++;
            }
        }
    }
    //Reduce creep count when a connected creep dies
    public void CreepDied() {
        activeCreepCount -= 1;
    }
    //Getters
    public bool GetActive() {
        if (activeCreepCount > 0) return true;
        return false;
    }
}
