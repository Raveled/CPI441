using UnityEngine;
using System.Collections;

public class Mosquito : MonoBehaviour
{
    [Header("Blood Meter - Passive (Blood Energy)")]
    [SerializeField] private float maxBloodMeter = 100f;
    [SerializeField] private float bloodMeterGainOnBasicHit = 5f;
    [SerializeField] private float bloodMeterGainOnPlayerHit = 10f;
    [SerializeField] private float bloodMeterDecayPerSecond = 0f;
    [SerializeField] private float extraDamagePerBloodUnit = 0.1f;

    [Header("Quick Poke (Ability 2)")]
    [SerializeField] private int quickPokeBaseDamage = 5;
    [SerializeField] private float quickPokeCooldown = 2f;
    [SerializeField] private float quickPokeBloodGain = 10f;
    [SerializeField] private float quickPokeBloodGainPlayer = 20f;

    [Header("Glob Shot (Ability 3)")]
    [SerializeField] private GameObject globProjectilePrefab;
    [SerializeField] private Transform globFirePoint;
    [SerializeField] private float globBaseDamage = 10f;
    [SerializeField] private float globBaseSpeed = 5f;
    [SerializeField] private float globMaxMeterUsageFraction = 0.5f;
    [SerializeField] private float globDamagePerBloodUnit = 0.3f;
    [SerializeField] private float globSizePerBloodUnit = 0.01f;

    [Header("Amp Up (Ultimate)")]
    [SerializeField] private float ampUpDuration = 5f;
    [SerializeField] private float ampUpInitialMoveMult = 2f;
    [SerializeField] private float ampUpInitialAttackSpeedMult = 2f;
    [SerializeField] private Color ampUpColor = Color.red;

    // Runtime state
    private float currentBloodMeter = 0f;
    private float quickPokeCooldownTimer = 0f;
    private float ampUpTimer = 0f;
    private float ampUpCurrentMoveMult = 1f;
    private float ampUpCurrentAttackSpeedMult = 1f;
    private Color originalColor;
    private MeshRenderer meshRenderer;
    public Entity entity;

    private void Awake()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        entity = GetComponent<Entity>();
        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;
    }

    private void Start()
    {
        if (entity != null)
        {
            // Optional: init blood based on entity stats
        }
    }

    private void Update()
    {
        // Blood decay
        if (bloodMeterDecayPerSecond > 0f && currentBloodMeter > 0f)
        {
            ModifyBloodMeter(-bloodMeterDecayPerSecond * Time.deltaTime);
        }

        // Quick poke cooldown
        if (quickPokeCooldownTimer > 0f)
            quickPokeCooldownTimer -= Time.deltaTime;

        UpdateAmpUp();
    }

    private void ModifyBloodMeter(float delta)
    {
        currentBloodMeter = Mathf.Clamp(currentBloodMeter + delta, 0f, maxBloodMeter);
    }

    public float GetBloodMeter01() => maxBloodMeter <= 0f ? 0f : currentBloodMeter / maxBloodMeter;

    // ========== BLOOD ENERGY PASSIVE ==========
    public int GetBasicAttackDamageWithBlood(int baseDamage)
    {
        float bonusDamage = currentBloodMeter * extraDamagePerBloodUnit;
        return Mathf.RoundToInt(baseDamage + bonusDamage);
    }

    public void OnBasicAttackHit(Entity target)
    {
        bool hitPlayer = target.GetType().Name == "Player"; // prototype check
        float gain = hitPlayer ? bloodMeterGainOnPlayerHit : bloodMeterGainOnBasicHit;
        ModifyBloodMeter(gain);
    }

    // ========== QUICK POKE ==========
    public bool TryQuickPoke(Entity target)
    {
        if (quickPokeCooldownTimer > 0f || target == null) return false;

        quickPokeCooldownTimer = quickPokeCooldown;
        int damage = GetBasicAttackDamageWithBlood(quickPokeBaseDamage);
        target.TakeDamage(damage, entity);

        bool hitPlayer = target.GetType().Name == "Player";
        float gain = hitPlayer ? quickPokeBloodGainPlayer : quickPokeBloodGain;
        ModifyBloodMeter(gain);

        return true;
    }

    // ========== GLOB SHOT ==========
    public void CastGlobShot()
    {
        if (globProjectilePrefab == null || globFirePoint == null || currentBloodMeter <= 0f) return;

        float maxUsage = maxBloodMeter * globMaxMeterUsageFraction;
        float bloodToUse = Mathf.Min(currentBloodMeter, maxUsage);
        ModifyBloodMeter(-bloodToUse);

        float damage = globBaseDamage + bloodToUse * globDamagePerBloodUnit;
        float sizeScale = 1f + bloodToUse * globSizePerBloodUnit;

        GameObject projGO = Instantiate(globProjectilePrefab, globFirePoint.position, globFirePoint.rotation);
        projGO.transform.localScale *= sizeScale;

        GlobProjectile proj = projGO.GetComponent<GlobProjectile>();
        if (proj != null)
        {
            proj.damage = Mathf.RoundToInt(damage);
            proj.speed = globBaseSpeed;
            proj.ownerEntity = entity;  // Pass Entity instead
        }
    }

    // ========== AMP UP ==========
    public void ActivateAmpUp()
    {
        if (ampUpTimer > 0f) return;

        ampUpTimer = ampUpDuration;
        ampUpCurrentMoveMult = ampUpInitialMoveMult;
        ampUpCurrentAttackSpeedMult = ampUpInitialAttackSpeedMult;

        if (meshRenderer != null)
            meshRenderer.material.color = ampUpColor;
    }

    private void UpdateAmpUp()
    {
        if (ampUpTimer <= 0f) return;

        ampUpTimer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(ampUpTimer / ampUpDuration);
        ampUpCurrentMoveMult = Mathf.Lerp(ampUpInitialMoveMult, 1f, t);
        ampUpCurrentAttackSpeedMult = Mathf.Lerp(ampUpInitialAttackSpeedMult, 1f, t);

        if (ampUpTimer <= 0f)
        {
            if (meshRenderer != null)
                meshRenderer.material.color = originalColor;
        }
    }

    // Multiplier getters for movement/attack scripts
    public float GetMoveSpeedMultiplier() => ampUpCurrentMoveMult;
    public float GetAttackSpeedMultiplier() => ampUpCurrentAttackSpeedMult;

    // Debug helpers
    [ContextMenu("Test Quick Poke")]
    private void TestQuickPoke() { /* find target via Physics.Raycast */ }
}
