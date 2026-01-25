using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

//Superclass for all projectiles
public class Projectile : MonoBehaviour
{
    //Info when spawned
    protected Entity owner;
    protected int damage;
    protected Entity.Team team;
    protected List<Entity.Team> enemyTeams;

    //Components
    protected Rigidbody rb;
    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemyTeams = new List<Entity.Team>();
    }
    //Called by the script that spawns this object
    public virtual void SpawnSetup(Entity owner, int damage, Vector3 direction, float speed)
    {
        Debug.Log(owner.name);

        this.owner = owner;
        this.damage = damage;
        team = owner.GetTeam();
        rb.linearVelocity = direction.normalized * speed;

        if (team == Entity.Team.TEAM1)
        {
            enemyTeams.Add(Entity.Team.TEAM2);
            enemyTeams.Add(Entity.Team.NEUTRAL);
        } else if(team == Entity.Team.TEAM2)
        {
            enemyTeams.Add(Entity.Team.TEAM1);
            enemyTeams.Add(Entity.Team.NEUTRAL);
        } else if(team == Entity.Team.NEUTRAL)
        {
            enemyTeams.Add(Entity.Team.TEAM1);
            enemyTeams.Add(Entity.Team.TEAM2);
        }
    }
}
