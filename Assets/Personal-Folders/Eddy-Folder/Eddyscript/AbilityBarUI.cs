using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class AbilityBarUI : MonoBehaviour
{
    [Serializable]
    public sealed class AbilitySlot
    {
        public string name;
        public string displayKey;

        public Image icon;
        public Image cooldownOverlay;
        public Image passiveFillReveal;
        public TMP_Text keyLabel;

        public float defaultCooldown = 5f;
        public bool usePassiveFill;

        [HideInInspector] public float remainingCooldown;
        [HideInInspector] public float totalCooldown;
        [HideInInspector] public float passiveNormalized;
    }

    [Header("Slots")]
    [SerializeField] private AbilitySlot[] slots = new AbilitySlot[4];

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color coolingDownColor = new(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color passiveEmptyColor = new(0.35f, 0.35f, 0.35f, 1f);

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackIcon;

    private void Awake()
    {
        InitializeVisualState();
        InitializeKeyLabels();
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
            return;

        UseAbility(slotIndex, slots[slotIndex].defaultCooldown);
    }

    public void UseAbility(int slotIndex, float cooldownSeconds)
    {
        if (!IsValidSlot(slotIndex))
            return;

        AbilitySlot slot = slots[slotIndex];
        if (slot == null || slot.usePassiveFill)
            return;

        if (cooldownSeconds <= 0f)
        {
            SetReadyVisual(slot);
            return;
        }

        slot.totalCooldown = cooldownSeconds;
        slot.remainingCooldown = cooldownSeconds;
        SetCooldownVisual(slot);
    }

    public void SetIcons(Sprite first, Sprite second, Sprite third, Sprite fourth)
    {
        SetIcon(0, first);
        SetIcon(1, second);
        SetIcon(2, third);
        SetIcon(3, fourth);
    }

    public void SetKeybinds(string first, string second, string third, string fourth)
    {
        SetKeybind(0, first);
        SetKeybind(1, second);
        SetKeybind(2, third);
        SetKeybind(3, fourth);
    }

    public void SetKeybind(int slotIndex, string keyText)
    {
        if (!IsValidSlot(slotIndex))
            return;

        AbilitySlot slot = slots[slotIndex];
        slot.displayKey = keyText;

        if (slot.keyLabel != null)
            slot.keyLabel.text = keyText;
    }

    public void SetIcon(int slotIndex, Sprite sprite)
    {
        if (!IsValidSlot(slotIndex))
            return;

        AbilitySlot slot = slots[slotIndex];
        Sprite finalSprite = sprite != null ? sprite : fallbackIcon;
        if (finalSprite == null)
            return;

        if (slot.icon != null)
            slot.icon.sprite = finalSprite;

        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.sprite = finalSprite;

        if (slot.passiveFillReveal != null)
            slot.passiveFillReveal.sprite = finalSprite;
    }

    public void SetPassiveFill(int slotIndex, float normalized)
    {
        if (!IsValidSlot(slotIndex))
            return;

        AbilitySlot slot = slots[slotIndex];
        if (slot == null || !slot.usePassiveFill)
            return;

        slot.passiveNormalized = Mathf.Clamp01(normalized);

        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.enabled = false;

        if (slot.icon != null)
            slot.icon.color = passiveEmptyColor;

        if (slot.passiveFillReveal != null)
        {
            slot.passiveFillReveal.enabled = true;
            slot.passiveFillReveal.fillAmount = slot.passiveNormalized;
            slot.passiveFillReveal.color = readyColor;
        }
    }

    private void InitializeVisualState()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            AbilitySlot slot = slots[i];
            if (slot == null)
                continue;

            slot.remainingCooldown = 0f;
            slot.totalCooldown = Mathf.Max(0.01f, slot.defaultCooldown);
            slot.passiveNormalized = 0f;

            if (slot.cooldownOverlay != null)
            {
                slot.cooldownOverlay.type = Image.Type.Filled;
                slot.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                slot.cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                slot.cooldownOverlay.fillClockwise = false;
            }

            if (slot.passiveFillReveal != null)
            {
                slot.passiveFillReveal.type = Image.Type.Filled;
                slot.passiveFillReveal.fillMethod = Image.FillMethod.Vertical;
                slot.passiveFillReveal.fillOrigin = (int)Image.OriginVertical.Bottom;
                slot.passiveFillReveal.fillAmount = 0f;
                slot.passiveFillReveal.enabled = slot.usePassiveFill;
            }

            if (slot.usePassiveFill)
                SetPassiveFill(i, 0f);
            else
                SetReadyVisual(slot);
        }
    }

    private void InitializeKeyLabels()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            AbilitySlot slot = slots[i];
            if (slot == null || slot.keyLabel == null)
                continue;

            slot.keyLabel.text = slot.displayKey;
        }
    }

    private void TickSlot(AbilitySlot slot, float deltaTime)
    {
        if (slot == null || slot.usePassiveFill)
            return;

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
            slot.icon.color = Color.Lerp(readyColor, coolingDownColor, normalized);

        if (slot.remainingCooldown <= 0f)
            SetReadyVisual(slot);
    }

    private void SetReadyVisual(AbilitySlot slot)
    {
        if (slot == null)
            return;

        slot.remainingCooldown = 0f;

        if (slot.icon != null)
            slot.icon.color = readyColor;

        if (slot.cooldownOverlay != null)
        {
            slot.cooldownOverlay.fillAmount = 0f;
            slot.cooldownOverlay.enabled = false;
        }

        if (slot.passiveFillReveal != null && !slot.usePassiveFill)
        {
            slot.passiveFillReveal.fillAmount = 0f;
            slot.passiveFillReveal.enabled = false;
        }
    }
    public void SetSlotPassiveMode(int slotIndex, bool usePassiveFill)
    {
        if (!IsValidSlot(slotIndex))
            return;

        AbilitySlot slot = slots[slotIndex];
        if (slot == null)
            return;

        slot.usePassiveFill = usePassiveFill;

        if (usePassiveFill)
            SetPassiveFill(slotIndex, slot.passiveNormalized);
        else
            SetReadyVisual(slot);
    }
    private void SetCooldownVisual(AbilitySlot slot)
    {
        if (slot == null)
            return;

        float normalized = slot.totalCooldown <= 0f
            ? 0f
            : slot.remainingCooldown / slot.totalCooldown;

        if (slot.icon != null)
            slot.icon.color = Color.Lerp(readyColor, coolingDownColor, normalized);

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