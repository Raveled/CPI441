using UnityEngine;

[CreateAssetMenu(menuName = "StatBlock", order = 100)]
public class SO_EntityStatBlock : ScriptableObject
{
    //Base stats
    [SerializeField] int goldReward;
    [SerializeField] int baseHitPoints;
    [SerializeField] float baseMoveSpeed;
    [SerializeField] int baseAttackPower;
    [SerializeField] float baseDefaultAttackCooldown;

    //Getters (Ex. x = statblock.BaseHitPoints;)
    public int goldReward { get { return goldReward; } }
    public int BaseHitPoints { get { return baseHitPoints; } }
    public float BaseMoveSpeed { get { return baseMoveSpeed; } }
    public int BaseAttackPower { get { return baseAttackPower; } }
    public float BaseDefaultAttackCooldown { get { return baseDefaultAttackCooldown; } }
}
