using UnityEngine;
using System.Collections.Generic;

public class NPEDetectLogic : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Green Circle")]
    [SerializeField] bool showDetectRange = false;
    [Tooltip("Always equal to the range of the sphere collider")]
    [SerializeField] float detectRange = 0f;
    [SerializeField] List<Entity.Team> enemyTeams;
    [SerializeField] List<Entity> enemiesInRange = null;
    private void Start() {
        //Init
        enemiesInRange = new List<Entity>();
        detectRange = GetComponent<SphereCollider>().radius;
    }
    private void OnTriggerEnter(Collider other) {
        //If collision is an enemy entity, add it to the list enemiesInRange 
        if(other.gameObject.TryGetComponent<Entity>(out Entity e)) {
            if(enemyTeams.Contains(e.GetTeam())) {
                enemiesInRange.Add(e);
            }
        }
    }
    private void OnTriggerExit(Collider other) {
        //If collision is an enemy entity in range, remove it from the list enemiesInRange
        if (other.gameObject.TryGetComponent<Entity>(out Entity e)) {
            if (enemiesInRange.Contains(e)) enemiesInRange.Remove(e);
        }
    }
    private void OnDrawGizmos() {
        if (showDetectRange) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectRange);
        }
    }

    //Setter
    public List<Entity> GetEnemiesInRange() {
        return enemiesInRange;
    }
    //Getter
    public void SetEnemyTeams(List<Entity.Team> t) {
        enemyTeams = t;
    }
}
