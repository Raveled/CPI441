using UnityEngine;
using System.Collections;
using PurrNet;

public class Mosquito : NetworkBehaviour
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

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;
    }

    private void Update()
    {
        if (bloodMeterDecayPerSecond > 0f && currentBloodMeter > 0f)
            ModifyBloodMeter(-bloodMeterDecayPerSecond * Time.deltaTime);

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
        Debug.Log($"[Mosquito] CastBloodShot called. isServer={isServer}, isSpawned={isSpawned}, entity={entity?.name}");
        PlayBloodShotAnim();

        int damage = GetBasicAttackDamageWithBlood(bloodShotBaseDamage);
        Debug.Log($"[Mosquito] Damage calculated: {damage}. Routing to server...");

        if (isServer)
        {
            Debug.Log("[Mosquito] IS server — spawning directly.");
            ServerSpawnBloodShot(bloodShotFirePoint.position, bloodShotFirePoint.rotation, damage);
        }
        else
        {
            Debug.Log("[Mosquito] NOT server — sending ServerRpc.");
            ServerSpawnBloodShotRpc(bloodShotFirePoint.position, bloodShotFirePoint.rotation, damage);
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnBloodShotRpc(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Mosquito] ServerSpawnBloodShotRpc received on server. damage={damage}");
        ServerSpawnBloodShot(position, rotation, damage);
    }

    private void ServerSpawnBloodShot(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Mosquito] ServerSpawnBloodShot — prefab={bloodShotProjectilePrefab}, firePoint={bloodShotFirePoint}, networkManager={networkManager}");

        if (bloodShotProjectilePrefab == null) { Debug.LogError("[Mosquito] bloodShotProjectilePrefab is NULL!"); return; }
        if (bloodShotFirePoint == null) { Debug.LogError("[Mosquito] bloodShotFirePoint is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Mosquito] networkManager is NULL!"); return; }

        GameObject projGO = Instantiate(bloodShotProjectilePrefab, position, rotation);
        Debug.Log($"[Mosquito] Instantiated projectile: {projGO.name}. PurrNet will auto-sync via NetworkBehaviour.");

        BloodShotProjectile proj = projGO.GetComponent<BloodShotProjectile>();
        if (proj == null) { Debug.LogError("[Mosquito] BloodShotProjectile component NOT found on prefab!"); return; }

        proj.syncDamage.value = damage;
        proj.syncSpeed.value = bloodShotSpeed;
        proj.syncMaxRange.value = bloodShotRange;
        proj.syncOwnerID.value = entity.GetNetworkID(isServer);
        Debug.Log($"[Mosquito] Projectile configured — damage={damage}, speed={bloodShotSpeed}, range={bloodShotRange}, ownerID={entity.GetNetworkID(isServer)}");
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
        if (quickPokeCooldownTimer > 0f || target == null || entity == null)
            return false;

        quickPokeCooldownTimer = quickPokeCooldown;
        PlayQuickPokeAnim();

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
        PlayGlobShotAnim();

        float maxUsage = maxBloodMeter * globMaxMeterUsageFraction;
        float bloodToUse = Mathf.Min(currentBloodMeter, maxUsage);
        if (bloodToUse <= 0f) return;

        ModifyBloodMeter(-bloodToUse);
        float damage = globBaseDamage + bloodToUse * globDamagePerBloodUnit;
        float sizeScale = 1f + bloodToUse * globSizePerBloodUnit;

        if (isServer)
            ServerSpawnGlob(globFirePoint.position, globFirePoint.rotation, damage, sizeScale);
        else
            ServerSpawnGlobRpc(globFirePoint.position, globFirePoint.rotation, damage, sizeScale);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnGlobRpc(Vector3 position, Quaternion rotation, float damage, float sizeScale)
    {
        ServerSpawnGlob(position, rotation, damage, sizeScale);
    }

    private void ServerSpawnGlob(Vector3 position, Quaternion rotation, float damage, float sizeScale)
    {
        if (globProjectilePrefab == null || globFirePoint == null) return;

        GameObject projGO = Instantiate(globProjectilePrefab, position, rotation);
        projGO.transform.localScale *= sizeScale;

        GlobProjectile proj = projGO.GetComponent<GlobProjectile>();
        if (proj != null)
        {
            proj.syncDamage.value = Mathf.RoundToInt(damage);
            proj.syncSpeed.value = globBaseSpeed;
            proj.syncOwnerID.value = entity.GetNetworkID(isServer);
        }
    }

    // ========== AMP UP - ULTIMATE ==========
    public void ActivateAmpUp()
    {
        if (ampUpTimer > 0f) return;

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
        if (animator != null) animator.SetTrigger("BloodShot");
    }
    private void PlayQuickPokeAnim()
    {
        if (animator != null) animator.SetTrigger("QuickPoke");
    }
    private void PlayGlobShotAnim()
    {
        if (animator != null) animator.SetTrigger("GlobShot");
    }
    private void PlayAmpUpAnimation()
    {
        if (animator != null) animator.SetTrigger("AmpUp");
    }
    public void PlayDeathAnimation()
    {
        if (animator != null) animator.SetTrigger("Death");
    }

    public float GetMoveSpeedMultiplier() => ampUpCurrentMoveMult;
    public float GetAttackSpeedMultiplier() => ampUpCurrentAttackSpeedMult;

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