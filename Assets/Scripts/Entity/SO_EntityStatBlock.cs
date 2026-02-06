using UnityEngine;

[CreateAssetMenu(menuName = "StatBlock", order = 100)]
public class SO_EntityStatBlock : ScriptableObject
{
    //Base stats
    [SerializeField] int baseHitPoints;
    [SerializeField] float baseMoveSpeed;
    [SerializeField] int baseAttackPower;
    [SerializeField] float baseDefaultAttackCooldown;
    [SerializeField] int rewardGold = 0;
    [SerializeField] int rewardXP = 0;

    //Getters (Ex. x = statblock.BaseHitPoints;)
    public int BaseHitPoints { get { return baseHitPoints; } set { baseHitPoints = value; } }
    public float BaseMoveSpeed { get { return baseMoveSpeed; } set { baseMoveSpeed = value; } }
    public int BaseAttackPower { get { return baseAttackPower; } set { baseAttackPower = value; } }
    public float BaseDefaultAttackCooldown { get { return baseDefaultAttackCooldown; } set { baseDefaultAttackCooldown = value; } }
    public int RewardGold { get { return rewardGold; } set { rewardGold = value; } }
    public int RewardXP { get { return rewardXP; } set { rewardXP = value; } }
}
