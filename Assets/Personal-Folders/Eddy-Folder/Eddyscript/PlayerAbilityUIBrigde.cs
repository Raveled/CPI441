using UnityEngine;

public sealed class PlayerAbilityUIBridge : MonoBehaviour
{
    [SerializeField] private AbilityBarUI abilityBar;

    private void Awake()
    {
        if (abilityBar == null)
        {
            abilityBar = FindFirstObjectByType<AbilityBarUI>();
        }
    }

    public bool CanUseAbility(int slotIndex)
    {
        return abilityBar != null && abilityBar.IsReady(slotIndex);
    }

    public void NotifyAbilityUsed(int slotIndex, float cooldownSeconds)
    {
        if (abilityBar == null)
        {
            return;
        }

        abilityBar.UseAbility(slotIndex, cooldownSeconds);
    }
}