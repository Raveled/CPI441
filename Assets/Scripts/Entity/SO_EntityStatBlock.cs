using UnityEngine;

[CreateAssetMenu(menuName = "StatBlock", order = 100)]
public class SO_EntityStatBlock : ScriptableObject
{
    //Base stats
    [SerializeField] int baseHitPoints;
    [SerializeField] float baseMoveSpeed;
    [SerializeField] int baseAttackPower;
    [SerializeField] float baseDefaultAttackCooldown;

    //Getters (Ex. x = statblock.BaseHitPoints;)
    public int BaseHitPoints { get { return baseHitPoints; } set { baseHitPoints = value; } }
    public float BaseMoveSpeed { get { return baseMoveSpeed; } set { baseMoveSpeed = value; } }
    public int BaseAttackPower { get { return baseAttackPower; } set { baseAttackPower = value; } }
    public float BaseDefaultAttackCooldown { get { return baseDefaultAttackCooldown; } set { baseDefaultAttackCooldown = value; } }
}
