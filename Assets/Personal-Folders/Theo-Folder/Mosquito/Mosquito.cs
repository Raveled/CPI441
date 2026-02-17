using UnityEngine;
using System.Collections;

public class Mosquito : MonoBehaviour
{
    [Header("Basic Attack - Blood Shot")]
    [SerializeField] private GameObject bloodShotProjectilePrefab;
    [SerializeField] private Transform bloodShotFirePoint;
    [SerializeField] private int bloodShotBaseDamage = 8;
    [SerializeField] private float bloodShotSpeed = 12f;
    [SerializeField] private float bloodShotRange = 8f;

    [Header("Blood Energy - Ability 1 (Passive)")]
    [SerializeField] private float maxBloodMeter = 100f;
    [SerializeField] private float bloodMeterGainOnBasicHit = 5f;
    [SerializeField] private float bloodMeterGainOnPlayerHit = 10f;
    [SerializeField] private float bloodMeterDecayPerSecond = 0f;
    [SerializeField] private float extraDamagePerBloodUnit = 0.1f;

    [Header("Quick Poke - Ability 2")]
    [SerializeField] private int quickPokeBaseDamage = 5;
    [SerializeField] private float quickPokeCooldown = 2f;
    [SerializeField] private float quickPokeBloodGain = 10f;
    [SerializeField] private float quickPokeBloodGainPlayer = 20f;
    private float quickPokeCooldownTimer = 0f;

    [Header("Glob Shot - Ability 3")]
    [SerializeField] private GameObject globProjectilePrefab;
    [SerializeField] private Transform globFirePoint;
    [SerializeField] private float globBaseDamage = 10f;
    [SerializeField] private float globBaseSpeed = 5f;
    [SerializeField] private float globMaxMeterUsageFraction = 0.5f;
    [SerializeField] private float globDamagePerBloodUnit = 0.3f;
    [SerializeField] private float globSizePerBloodUnit = 0.01f;

    [Header("Amp Up - Ultimate")]
    [SerializeField] private float ampUpDuration = 5f;
    [SerializeField] private float ampUpInitialMoveMult = 2f;
    [SerializeField] private float ampUpInitialAttackSpeedMult = 2f;
    [SerializeField] private Color ampUpColor = Color.red;

    // Animator functionality
    [Header("Animator")]
    [SerializeField] private Animator animator;

    // Runtime state
    private float currentBloodMeter = 0f;
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

        // Cache animator from Mosquito Rigged child
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;
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

    // ========== BASIC ATTACK - BLOOD SHOT ==========
    public void CastBloodShot()
    {
        Debug.Log("Casting Blood Shot");
        PlayBloodShotAnim();

        if (bloodShotProjectilePrefab == null || bloodShotFirePoint == null)
            return;

        Entity shooter = entity ?? GetComponent<Entity>();
        int damage = GetBasicAttackDamageWithBlood(bloodShotBaseDamage);

        GameObject projGO = Instantiate(bloodShotProjectilePrefab, bloodShotFirePoint.position, bloodShotFirePoint.rotation);
        BloodShotProjectile proj = projGO.GetComponent<BloodShotProjectile>();
        if (proj != null)
        {
            proj.ownerEntity = shooter;
            proj.damage = damage;
            proj.speed = bloodShotSpeed;
            proj.maxRange = bloodShotRange;
        }
    }

    // ========== BLOOD ENERGY PASSIVE (Ability 1) ==========
    public int GetBasicAttackDamageWithBlood(int baseDamage)
    {
        float bonusDamage = currentBloodMeter * extraDamagePerBloodUnit;
        return Mathf.RoundToInt(baseDamage + bonusDamage);
    }

    public void OnBasicAttackHit(Entity target)
    {
        bool hitPlayer = target.GetType().Name == "Player";
        float gain = hitPlayer ? bloodMeterGainOnPlayerHit : bloodMeterGainOnBasicHit;
        ModifyBloodMeter(gain);
    }

    // ========== QUICK POKE - ABILITY 2 ==========
    public bool TryQuickPoke(Entity target)
    {
        Debug.Log("Attempting Quick Poke on target: " + (target != null ? target.name : "null"));
        Debug.Log("Quick Poke Cooldown Timer: " + quickPokeCooldownTimer);
        Debug.Log("Current Blood Meter: " + currentBloodMeter);

        if (quickPokeCooldownTimer > 0f || target == null || entity == null)
            return false;

        quickPokeCooldownTimer = quickPokeCooldown;
        PlayQuickPokeAnim(); // Quick poke uses attack anim

        int damage = GetBasicAttackDamageWithBlood(quickPokeBaseDamage);
        target.TakeDamage(damage, entity);

        bool hitPlayer = target.GetType().Name == "Player";
        float gain = hitPlayer ? quickPokeBloodGainPlayer : quickPokeBloodGain;
        ModifyBloodMeter(gain);

        return true;
    }

    // ========== GLOB SHOT - ABILITY 3 ==========
    public void CastGlobShot()
    {
        Debug.Log("Attempting to Cast Glob Shot - Blood: " + currentBloodMeter);
        PlayGlobShotAnim();

        if (globProjectilePrefab == null || globFirePoint == null)
            return;

        Entity shooter = entity ?? GetComponent<Entity>();
        float maxUsage = maxBloodMeter * globMaxMeterUsageFraction;
        float bloodToUse = Mathf.Min(currentBloodMeter, maxUsage);

        if (bloodToUse <= 0f) return;

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
            proj.ownerEntity = shooter;
        }
    }

    // ========== AMP UP - ULTIMATE ==========
    public void ActivateAmpUp()
    {
        Debug.Log("Attempting to Activate Amp Up");
        if (ampUpTimer > 0f)
        {
            Debug.Log("Amp Up already active!");
            return;
        }

        ampUpTimer = ampUpDuration;
        ampUpCurrentMoveMult = ampUpInitialMoveMult;
        ampUpCurrentAttackSpeedMult = ampUpInitialAttackSpeedMult;

        PlayAmpUpAnimation();

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

    // ========== ANIMATOR METHODS ==========
    private void PlayBloodShotAnim()
    {
        if (animator != null)
            animator.SetTrigger("BloodShot");
    }
    private void PlayQuickPokeAnim()
    {
        if (animator != null)
            animator.SetTrigger("QuickPoke");
    }
    private void PlayGlobShotAnim()
    {
        if (animator != null)
            animator.SetTrigger("GlobShot");
    }

    private void PlayAmpUpAnimation()
    {
        if (animator != null)
            animator.SetTrigger("AmpUp");
    }

    public void PlayDeathAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Death");
    }

    // Multiplier getters for movement/attack scripts
    public float GetMoveSpeedMultiplier() => ampUpCurrentMoveMult;
    public float GetAttackSpeedMultiplier() => ampUpCurrentAttackSpeedMult;

    // Debug helpers
    [ContextMenu("Test Blood Shot")]
    private void TestBloodShot() => CastBloodShot();

    [ContextMenu("Test Quick Poke")]
    private void TestQuickPoke()
    {
        Entity target = FindNearestEnemy();
        if (target != null) TryQuickPoke(target);
    }

    [ContextMenu("Test Glob Shot")]
    private void TestGlobShot() => CastGlobShot();

    [ContextMenu("Test Amp Up")]
    private void TestAmpUp() => ActivateAmpUp();

    private Entity FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 5f);
        Entity nearest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            Entity enemy = hit.GetComponent<Entity>();
            if (enemy != null && entity != null && enemy.GetTeam() != entity.GetTeam())
            {
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = enemy;
                }
            }
        }
        return nearest;
    }
}