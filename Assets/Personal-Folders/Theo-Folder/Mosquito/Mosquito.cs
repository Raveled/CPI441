using UnityEngine;
using System.Collections;
using PurrNet;
using UnityEngine.EventSystems;

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

    [Header("Quick Poke - Ability 2 Setup")]
    [SerializeField] private float quickPokeRange = 2f;
    [SerializeField] private Transform quickPokeOrigin = null;

    [Header("Glob Shot - Ability 3")]
    [SerializeField] private GameObject globProjectilePrefab;
    [SerializeField] private Transform globFirePoint;
    [SerializeField] private float globBaseDamage = 10f;
    [SerializeField] private float globBaseSpeed = 5f;
    [SerializeField] private float globMaxMeterUsageFraction = 0.5f;
    [SerializeField] private float globDamagePerBloodUnit = 0.3f;
    [SerializeField] private float globSizePerBloodUnit = 0.01f;

    [Header("Glob Shot Threshold")]
    [SerializeField] private float globShotMinBloodThreshold = 10f;

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
    private Color originalColor;
    private Renderer meshRenderer;
    [SerializeField] public Player player;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        meshRenderer = GetComponentInChildren<Renderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;

        ServerTestRpc();
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerTestRpc()
    {
        Debug.Log("Test Server RPC for PlayerID: " + player.GetPlayerID());
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

    // *** DEBUG THEO *** //
    protected override void OnSpawned()
    {
        base.OnSpawned();
        Debug.Log($"[Mosquito] OnSpawned | name={gameObject.name} | isOwner={isOwner} | isController={isController} | isServer={isServer} | owner={owner} | localPlayer={networkManager?.localPlayer}");

        // Use owner == localPlayer instead of isOwner — isOwner returns true for all
        // objects on the host/server so it cannot distinguish which character belongs
        // to the local player. This compares the assigned owner PlayerID directly.
        if (networkManager != null && owner == networkManager.localPlayer)
        {
            MosquitoInputTester inputTester = GetComponent<MosquitoInputTester>();
            if (inputTester != null)
                inputTester.EnableInput();
        }
    }

    // ========== BASIC ATTACK - BLOOD SHOT ==========
    public void CastBloodShot()
    {
        if (!isController) return;
        Debug.Log($"[Mosquito] CastBloodShot on {gameObject.name} | isController={isController} | isOwner={isOwner} | isServer={isServer}");

        // FIX: Play animation through server so all clients see it
        PlayBloodShotAnimServerRpc();

        int damage = GetBasicAttackDamageWithBlood(bloodShotBaseDamage);
        Debug.Log($"[Mosquito] Damage calculated: {damage}. Spawning projectile...");

        // FIX: Use NetworkManager.main.Spawn instead of ServerRpc — same pattern as Player.cs
        SpawnBloodShot(bloodShotFirePoint.position, bloodShotFirePoint.rotation, damage);
    }

    private void SpawnBloodShot(Vector3 position, Quaternion rotation, int damage)
    {
        if (bloodShotProjectilePrefab == null) { Debug.LogError("[Mosquito] bloodShotProjectilePrefab is NULL!"); return; }
        if (bloodShotFirePoint == null) { Debug.LogError("[Mosquito] bloodShotFirePoint is NULL!"); return; }

        // Use prediction system to spawn - same pattern as TowerProjectile
        PurrNet.Prediction.PredictionManager predictionManager = UnityEngine.Object.FindFirstObjectByType<PurrNet.Prediction.PredictionManager>();
        if (predictionManager == null) { Debug.LogError("[Mosquito] PredictionManager not found!"); return; }

        PurrNet.Prediction.PredictedObjectID? projId = predictionManager.hierarchy.Create(bloodShotProjectilePrefab, position, rotation);
        if (!projId.HasValue) { Debug.LogError("[Mosquito] Failed to create projectile via hierarchy!"); return; }

        GameObject projGO = predictionManager.hierarchy.GetGameObject(projId);
        if (projGO == null) { Debug.LogError("[Mosquito] Failed to get projectile GameObject!"); return; }

        BloodShotProjectile proj = projGO.GetComponent<BloodShotProjectile>();
        if (proj == null) { Debug.LogError("[Mosquito] BloodShotProjectile component NOT found!"); return; }

        proj.SpawnSetup(entity, damage, bloodShotFirePoint.forward, bloodShotSpeed);
        Debug.Log($"[Mosquito] Spawned projectile via hierarchy - damage={damage}, speed={bloodShotSpeed}");
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
        Debug.Log($"[Mosquito] Blood gained: +{gain:F1} - current blood: {currentBloodMeter:F1}/{maxBloodMeter}");
    }

    // ========== QUICK POKE - ABILITY 2 ==========
    public bool TryQuickPoke()
    {
        if (!isController) return false;
        if (quickPokeCooldownTimer > 0f || entity == null)
        {
            Debug.Log($"[Mosquito] Quick Poke blocked - cooldown: {quickPokeCooldownTimer:F2}s remaining");
            return false;
        }

        quickPokeCooldownTimer = quickPokeCooldown;

        // FIX: Play animation through server so all clients see it
        PlayQuickPokeAnimServerRpc();

        if (isServer)
            ApplyQuickPoke();
        else
            ApplyQuickPokeServerRpc();

        return true;
    }

    [ServerRpc(requireOwnership: false)]
    private void ApplyQuickPokeServerRpc()
    {
        ApplyQuickPoke();
    }

    private void ApplyQuickPoke()
    {
        Vector3 origin = quickPokeOrigin != null ? quickPokeOrigin.position : transform.position;
        Debug.Log($"[Mosquito] Quick Poke - overlap sphere at {origin}, range={quickPokeRange}");

        Collider[] hits = Physics.OverlapSphere(origin, quickPokeRange);
        int hitCount = 0;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(player.transform) || hit.gameObject == player.gameObject)
                continue;

            Entity target = hit.GetComponent<Entity>();
            if (target == null || target.GetIsDead()) continue;
            if (!player.GetEnemyTeams().Contains(target.GetTeam())) continue;

            int damage = GetBasicAttackDamageWithBlood(quickPokeBaseDamage);
            Debug.Log($"[Mosquito] Quick Poke hit {target.name} for {damage} damage!");
            target.TakeDamage(damage, player);

            bool hitPlayer = target.GetType().Name == "Player";
            float gain = hitPlayer ? quickPokeBloodGainPlayer : quickPokeBloodGain;
            ModifyBloodMeter(gain);
            Debug.Log($"[Mosquito] Blood gained: +{gain:F1} - current blood: {currentBloodMeter:F1}/{maxBloodMeter}");

            hitCount++;
        }

        if (hitCount == 0)
            Debug.Log("[Mosquito] Quick Poke hit nothing.");
    }

    // ========== GLOB SHOT - ABILITY 3 ==========
    public void CastGlobShot()
    {
        if (!isController) return;
        if (currentBloodMeter < globShotMinBloodThreshold)
        {
            Debug.Log($"[Mosquito] Glob Shot blocked - blood {currentBloodMeter:F1} below threshold {globShotMinBloodThreshold}");
            return;
        }

        // FIX: Play animation through server so all clients see it
        PlayGlobShotAnimServerRpc();

        float maxUsage = maxBloodMeter * globMaxMeterUsageFraction;
        float bloodToUse = Mathf.Min(currentBloodMeter, maxUsage);
        if (bloodToUse <= 0f) return;

        ModifyBloodMeter(-bloodToUse);
        Debug.Log($"[Mosquito] Glob Shot fired - used {bloodToUse:F1} blood, remaining: {currentBloodMeter:F1}");
        float damage = globBaseDamage + bloodToUse * globDamagePerBloodUnit;
        float sizeScale = 1f + bloodToUse * globSizePerBloodUnit;

        // FIX: Use NetworkManager.main.Spawn instead of ServerRpc
        SpawnGlob(globFirePoint.position, globFirePoint.rotation, damage, sizeScale);
    }

    private void SpawnGlob(Vector3 position, Quaternion rotation, float damage, float sizeScale)
    {
        if (globProjectilePrefab == null || globFirePoint == null) return;

        GameObject projGO = Instantiate(globProjectilePrefab, position, rotation);
        NetworkManager.main.Spawn(projGO);

        projGO.transform.localScale *= sizeScale;

        GlobProjectile proj = projGO.GetComponent<GlobProjectile>();
        if (proj != null)
        {
            proj.syncDamage.value = Mathf.RoundToInt(damage);
            proj.syncSpeed.value = globBaseSpeed;
            proj.syncOwnerID.value = player.GetNetworkID(isServer);
        }
    }

    // ========== AMP UP - ULTIMATE ==========
    public void ActivateAmpUp()
    {
        if (!isController) return;
        if (ampUpTimer > 0f)
        {
            Debug.Log("[Mosquito] Amp Up blocked - already active.");
            return;
        }

        // FIX: Play animation through server so all clients see it
        PlayAmpUpAnimServerRpc();

        ampUpTimer = ampUpDuration;

        if (isServer)
            ApplyAmpUp();
        else
            ApplyAmpUpServerRpc();
    }

    [ServerRpc(requireOwnership: false)]
    private void ApplyAmpUpServerRpc()
    {
        ApplyAmpUp();
    }

    private void ApplyAmpUp()
    {
        Debug.Log($"[Mosquito] Amp Up activated! Duration={ampUpDuration}s, MoveMult={ampUpInitialMoveMult}, AttackMult={ampUpInitialAttackSpeedMult}");
        player.ModifyMoveSpeedMultiplier(ampUpInitialMoveMult, ampUpDuration);
        player.ModifyAttackPowerForSeconds(ampUpInitialAttackSpeedMult, ampUpDuration);
        SetAmpUpColorRpc(true);
    }

    private void UpdateAmpUp()
    {
        if (ampUpTimer <= 0f) return;

        ampUpTimer -= Time.deltaTime;

        if (ampUpTimer <= 0f)
        {
            ampUpTimer = 0f;
            if (isServer) SetAmpUpColorRpc(false);
            Debug.Log("[Mosquito] Amp Up expired.");
        }
    }

    [ObserversRpc]
    private void SetAmpUpColorRpc(bool active)
    {
        if (meshRenderer == null)
        {
            Debug.LogError("[Mosquito] meshRenderer is NULL! Check prefab hierarchy.");
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            Debug.Log($"Force search found meshRenderer={meshRenderer}");
        }
        meshRenderer.material.color = active ? ampUpColor : originalColor;
        Debug.Log($"[Mosquito] Amp Up color set to {(active ? "active" : "original")}.");
    }

    // ========== ANIMATOR METHODS ==========
    // FIX: Each animation now has a ServerRpc that triggers the ObserversRpc,
    // so the server properly broadcasts it to all clients instead of running locally only.

    [ServerRpc(requireOwnership: false)]
    private void PlayBloodShotAnimServerRpc() => PlayBloodShotAnim();

    [ObserversRpc]
    private void PlayBloodShotAnim()
    {
        if (animator != null) animator.SetTrigger("BloodShot");
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayQuickPokeAnimServerRpc() => PlayQuickPokeAnim();

    [ObserversRpc]
    private void PlayQuickPokeAnim()
    {
        if (animator != null) animator.SetTrigger("QuickPoke");
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayGlobShotAnimServerRpc() => PlayGlobShotAnim();

    [ObserversRpc]
    private void PlayGlobShotAnim()
    {
        if (animator != null) animator.SetTrigger("GlobShot");
    }

    [ServerRpc(requireOwnership: false)]
    private void PlayAmpUpAnimServerRpc() => PlayAmpUpAnimation();

    [ObserversRpc]
    private void PlayAmpUpAnimation()
    {
        if (animator != null) animator.SetTrigger("AmpUp");
    }

    [ObserversRpc]
    public void PlayDeathAnimation()
    {
        if (animator != null) animator.SetTrigger("Death");
    }

    public float GetMoveSpeedMultiplier() => player != null ? player.GetMoveSpeed() : 1f;
    public float GetAttackSpeedMultiplier() => player != null ? player.attackPower.value : 1f;

    [ContextMenu("Test Blood Shot")]
    private void TestBloodShot() => CastBloodShot();

    [ContextMenu("Test Quick Poke")]
    private void TestQuickPoke() => TryQuickPoke();

    [ContextMenu("Test Glob Shot")]
    private void TestGlobShot() => CastGlobShot();

    [ContextMenu("Test Amp Up")]
    private void TestAmpUp() => ActivateAmpUp();
}