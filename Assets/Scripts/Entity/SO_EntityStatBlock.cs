using UnityEngine;

[CreateAssetMenu(menuName = "StatBlock", order = 100)]
public class SO_EntityStatBlock : ScriptableObject
{
    //Base stats
    [SerializeField] int baseHitPoints;
    [SerializeField] float baseMoveSpeed;
    [SerializeField] float baseAcceleration;
    [SerializeField] float basePlanarDamping;
    [SerializeField] float baseJumpForce;
    [SerializeField] float baseJumpCooldown;
    [SerializeField] int baseAttackPower;
    [SerializeField] float baseDefaultAttackCooldown;
    [SerializeField] int rewardGold = 0;
    [SerializeField] int rewardXP = 0;

    //Getters (Ex. x = statblock.BaseHitPoints;)
    public int BaseHitPoints { get { return baseHitPoints; } set { baseHitPoints = value; } }
    public float BaseMoveSpeed { get { return baseMoveSpeed; } set { baseMoveSpeed = value; } }
    public float BaseAcceleration { get { return baseAcceleration; } set { baseAcceleration = value; } }
    public float BasePlanarDamping { get { return basePlanarDamping; } set { basePlanarDamping = value; } }
    public float BaseJumpForce { get { return baseJumpForce; } set { baseJumpForce = value; } }
    public float BaseJumpCooldown { get { return baseJumpCooldown; } set { baseJumpCooldown = value; } }
    public int BaseAttackPower { get { return baseAttackPower; } set { baseAttackPower = value; } }
    public float BaseDefaultAttackCooldown { get { return baseDefaultAttackCooldown; } set { baseDefaultAttackCooldown = value; } }
    public int RewardGold { get { return rewardGold; } set { rewardGold = value; } }
    public int RewardXP { get { return rewardXP; } set { rewardXP = value; } }
}
