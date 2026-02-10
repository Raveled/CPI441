using UnityEngine;
using System.Collections.Generic;

public class NPEDetectLogic : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Green Circle")]
    [SerializeField] bool showDetectRange = false;
    [Tooltip("Always equal to the range of the sphere collider")]
    float detectRange = 0f;
    [SerializeField] List<Entity> enemiesInRange = null;

    NonPlayerEntity npe = null;
    private void Start() {
        //Init
        npe = gameObject.transform.parent.GetComponent<NonPlayerEntity>();
        enemiesInRange = new List<Entity>();
        detectRange = GetComponent<SphereCollider>().radius;
    }
    private void OnTriggerEnter(Collider other) {
        //If collision is an enemy entity, add it to the list enemiesInRange 
        enemiesInRange.RemoveAll(e => e == null);
        if (npe.GetEnemyTeams().Count <= 0) return;

        if(other.gameObject.TryGetComponent<Entity>(out Entity e)) 
        {
            if (e == npe) return;
            if (e.GetTeam() == Entity.Team.NULL) return;
            if (e.GetTeam() == npe.GetTeam()) return;
            if(npe.GetEnemyTeams().Contains(e.GetTeam())) {
                enemiesInRange.Add(e);
            }  
        }
    }
    private void OnTriggerExit(Collider other) {
        //If collision is an enemy entity in range, remove it from the list enemiesInRange
        if (other.gameObject.TryGetComponent<Entity>(out Entity e)) {
            if (enemiesInRange.Contains(e)) {
                enemiesInRange.Remove(e);
                if(npe.GetTarget() == e) {
                    npe.ResetTarget();
                }
            }
        }
    }
    private void OnDrawGizmos() {
        if (showDetectRange) {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectRange);
        }
    }

    //Getters
    public List<Entity> GetEnemiesInRange() {
        return enemiesInRange;
    }

    //Setters
    public void SetNPE (NonPlayerEntity entity) {
        npe = entity;
    }
}
