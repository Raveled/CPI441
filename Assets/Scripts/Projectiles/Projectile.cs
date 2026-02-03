using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

//Superclass for all projectiles
public class Projectile : MonoBehaviour
{
    [Header("Projectile Setup")]
    [Tooltip("Must be set in inspector")]
    [SerializeField] protected Rigidbody rb;

    //Info when spawned
    [Header("Projectile Debug")]
    [SerializeField] protected Entity owner;
    [SerializeField] protected int damage;
    [SerializeField] protected List<Entity.Team> enemyTeams;
    [SerializeField] protected Entity target;
    protected virtual void Start() {
        rb = GetComponent<Rigidbody>();
    }
    //Called by the script that spawns this object
    public virtual void SpawnSetup(Entity owner, int damage, Vector3 direction, float speed, Entity target)
    {
        //Setup
        this.owner = owner;
        this.damage = damage;
        this.target = target;
        enemyTeams = owner.GetEnemyTeams();
        rb.linearVelocity = direction.normalized * speed;
    }
}
