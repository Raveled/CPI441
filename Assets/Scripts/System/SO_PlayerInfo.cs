using UnityEngine;

public class SO_PlayerInfo : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] int killCount = 0;
    [SerializeField] int deathCount = 0;
    [SerializeField] int assistCount = 0;

    //Getters+Setters
    public int KillCount { get { return killCount; } set { killCount = value; } }
    public int DeathCount { get { return deathCount; } set { deathCount = value; } }
    public int AssistCount { get { return assistCount; } set { assistCount = value; } }

}
