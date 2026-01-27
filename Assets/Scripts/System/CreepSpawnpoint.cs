using UnityEngine;

public class CreepSpawnpoint : MonoBehaviour
{
    //***Uses this object's transform position to spawn the creep

    //Fields
    [SerializeField] GameObject creepToSpawn;
    [Space]
    [Header("Creep Spawnpoint Debug")]
    [SerializeField] bool disableVisualOnStart = true;
    private void Start() {
        //Disable visual (used for editor only)
        if (disableVisualOnStart) {
            TryGetComponent<MeshRenderer>(out MeshRenderer m);
            m.enabled = false;
        }
    }
    //Getter
    public GameObject GetCreepType() {
        return creepToSpawn;
    }
}
