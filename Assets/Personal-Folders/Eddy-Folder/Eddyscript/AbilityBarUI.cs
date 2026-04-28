using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class AbilityBarUI : MonoBehaviour
{
    [Serializable]
    public sealed class AbilitySlot
    {
        public string name;
        public Image icon;
        public Image cooldownOverlay;
        public float defaultCooldown = 5f;

        [HideInInspector] public float remainingCooldown;
        [HideInInspector] public float totalCooldown;
    }

    [Header("Slots")]
    [SerializeField] private AbilitySlot[] slots = new AbilitySlot[3];

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color coolingDownColor = new(0.35f, 0.35f, 0.35f, 1f);

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackIcon;

    private void Awake()
    {
        InitializeVisualState();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        for (int i = 0; i < slots.Length; i++)
        {
            TickSlot(slots[i], deltaTime);
        }
    }

    public bool IsReady(int slotIndex)
    {
        return IsValidSlot(slotIndex) && slots[slotIndex].remainingCooldown <= 0f;
    }

    public void UseAbility(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return;
        }

        UseAbility(slotIndex, slots[slotIndex].defaultCooldown);
    }

    public void UseAbility(int slotIndex, float cooldownSeconds)
    {
        if (!IsValidSlot(slotIndex))
        {
            return;
        }

        AbilitySlot slot = slots[slotIndex];
        if (slot == null || cooldownSeconds <= 0f)
        {
            SetReadyVisual(slot);
            return;
        }

        slot.totalCooldown = cooldownSeconds;
        slot.remainingCooldown = cooldownSeconds;
        SetCooldownVisual(slot);
    }

    public void SetIcons(Sprite first, Sprite second, Sprite third)
    {
        SetIcon(0, first);
        SetIcon(1, second);
        SetIcon(2, third);
    }

    public void SetIcon(int slotIndex, Sprite sprite)
    {
        if (!IsValidSlot(slotIndex))
        {
            return;
        }

        AbilitySlot slot = slots[slotIndex];
        Sprite finalSprite = sprite != null ? sprite : fallbackIcon;

        if (slot.icon != null && finalSprite != null)
        {
            slot.icon.sprite = finalSprite;
        }

        if (slot.cooldownOverlay != null && finalSprite != null)
        {
            slot.cooldownOverlay.sprite = finalSprite;
        }
    }

    private void InitializeVisualState()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            AbilitySlot slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            slot.remainingCooldown = 0f;
            slot.totalCooldown = Mathf.Max(0.01f, slot.defaultCooldown);

            if (slot.cooldownOverlay != null)
            {
                slot.cooldownOverlay.type = Image.Type.Filled;
                slot.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                slot.cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                slot.cooldownOverlay.fillClockwise = false;
            }

            SetReadyVisual(slot);
        }
    }

    private void TickSlot(AbilitySlot slot, float deltaTime)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.remainingCooldown <= 0f)
        {
            SetReadyVisual(slot);
            return;
        }

        slot.remainingCooldown = Mathf.Max(0f, slot.remainingCooldown - deltaTime);

        float normalized = slot.totalCooldown <= 0f
            ? 0f
            : slot.remainingCooldown / slot.totalCooldown;

        if (slot.cooldownOverlay != null)
        {
            slot.cooldownOverlay.enabled = true;
            slot.cooldownOverlay.fillAmount = normalized;
        }

        if (slot.icon != null)
        {
            slot.icon.color = Color.Lerp(readyColor, coolingDownColor, normalized);
        }

        if (slot.remainingCooldown <= 0f)
        {
            SetReadyVisual(slot);
        }
    }

    private void SetReadyVisual(AbilitySlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.remainingCooldown = 0f;

        if (slot.icon != null)
        {
            slot.icon.color = readyColor;
        }

        if (slot.cooldownOverlay != null)
        {
            slot.cooldownOverlay.fillAmount = 0f;
            slot.cooldownOverlay.enabled = false;
        }
    }

    private void SetCooldownVisual(AbilitySlot slot)
    {
        if (slot == null)
        {
            return;
        }

        float normalized = slot.totalCooldown <= 0f
            ? 0f
            : slot.remainingCooldown / slot.totalCooldown;

        if (slot.icon != null)
        {
            slot.icon.color = Color.Lerp(readyColor, coolingDownColor, normalized);
        }

        if (slot.cooldownOverlay != null)
        {
            slot.cooldownOverlay.enabled = true;
            slot.cooldownOverlay.fillAmount = normalized;
            slot.cooldownOverlay.color = new Color(0f, 0f, 0f, 0.6f);
        }
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slots != null && slotIndex >= 0 && slotIndex < slots.Length;
    }
}