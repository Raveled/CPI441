using System;

[Serializable]
public struct StatModifiers
{
    public int healthBonus;
    public int attackDamageBonus;
    public float attackSpeedMultiplier;
    public float movementSpeedMultiplier;
    public int defenseBonus;

    public StatModifiers(
        int health = 0,
        int attack = 0,
        float attackSpeed = 1f,
        float moveSpeed = 1f,
        int defense = 0)
    {
        healthBonus = health;
        attackDamageBonus = attack;
        attackSpeedMultiplier = attackSpeed;
        movementSpeedMultiplier = moveSpeed;
        defenseBonus = defense;
    }

}