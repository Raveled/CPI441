using UnityEngine;
using System.Collections;
using PurrNet;
using PurrNet.Prediction;
using UnityEngine.EventSystems;
using System;
using Unity.VisualScripting;

public class Mosquito : NetworkBehaviour
{
    [Header("UI cooldown hook up")]
    [SerializeField] private AbilityBarUI abilityBar;

    [Header("Basic Attack - Blood Shot")]
    [SerializeField] private GameObject bloodShotProjectilePrefab;
    [SerializeField] private Transform bloodShotFirePoint;
    [SerializeField] private int bloodShotBaseDamage = 8;
    [SerializeField] private float bloodShotSpeed = 12f;
    [SerializeField] private float bloodShotRange = 8f;

    [Header("Basic Attack Cooldown")]
    [SerializeField] private float bloodShotCooldown = 0.35f;
    [SerializeField] private int bloodShotAbilityBarIndex = 0;
    [SerializeField] private bool startBloodShotCooldownLocallyOnInput = true;
    [SerializeField] private bool enableBloodShotDebugLogs = true;

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

    [Header("Quick Poke Cooldown")]
    [SerializeField] private int quickPokeAbilityBarIndex = 1;
    [SerializeField] private bool startQuickPokeCooldownLocallyOnInput = true;
    [SerializeField] private bool enableQuickPokeDebugLogs = true;

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
    [SerializeField] private float globShotCooldown = 5f;

    [Header("Glob Shot Cooldown")]
    [SerializeField] private int globShotAbilityBarIndex = 2;
    [SerializeField] private bool startGlobShotCooldownLocallyOnInput = true;
    [SerializeField] private bool enableGlobShotDebugLogs = true;

    [Header("Glob Shot Threshold")]
    [SerializeField] private float globShotMinBloodThreshold = 10f;

    [Header("Amp Up - Ultimate")]
    [SerializeField] private float ampUpDuration = 5f;
    [SerializeField] private float ampUpInitialMoveMult = 2f;
    [SerializeField] private float ampUpInitialAttackSpeedMult = 2f;
    [SerializeField] private Color ampUpColor = Color.red;

    [Header("Amp Up Cooldown Reduction")]
    [SerializeField] private float ampUpCooldownReductionMultiplier = 0.5f; // 50% cooldown reduction
    [SerializeField] private float ampUpBloodShotCooldownReduced = 0.175f; // 0.35 * 0.5
    [SerializeField] private float ampUpQuickPokeCooldownReduced = 1f; // 2 * 0.5
    [SerializeField] private float ampUpGlobShotCooldownReduced = 2.5f; // 5 * 0.5

    [Header("Amp Up Cooldown")]
    [SerializeField] private float ampUpCooldown = 12f;
    [SerializeField] private int ampUpAbilityBarIndex = 3;
    [SerializeField] private bool startAmpUpCooldownLocallyOnInput = true;
    [SerializeField] private bool enableAmpUpDebugLogs = true;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    // Runtime state
    private float currentBloodMeter = 0f;
    private float ampUpTimer = 0f;
    private Color originalColor;
    private Renderer meshRenderer;

    // Amp Up active flag for cooldown reduction
    private bool isAmpUpActive = false;

    [SerializeField] public Player player;
    [SerializeField] public MosquitoInputTester inputTester;

    // Local cooldown timers
    private float bloodShotCooldownTimer = 0f;
    private float quickPokeCooldownTimer = 0f;
    private float globShotCooldownTimer = 0f;
    private float ampUpCooldownTimer = 0f;

    // Server authoritative cooldown times
    private float bloodShotNextAllowedTimeServer = 0f;
    private float quickPokeNextAllowedTimeServer = 0f;
    private float globShotNextAllowedTimeServer = 0f;
    private float ampUpNextAllowedTimeServer = 0f;

    protected override void OnSpawned(bool asServer)
    {
        StartCoroutine(DelayedSpawn(asServer));
    }

    private IEnumerator DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(0.1f);

        base.OnSpawned();

        GameObject parentObject = transform.parent.gameObject;

        bloodShotFirePoint = parentObject.GetComponent<PredictedPlayerMovement>().firingPoint.transform;
        quickPokeOrigin = bloodShotFirePoint;
        globFirePoint = bloodShotFirePoint;

        if (animator == null)
            animator = parentObject.GetComponentInChildren<Animator>();

        if (meshRenderer == null)
            meshRenderer = parentObject.GetComponentInChildren<Renderer>();

        if (meshRenderer != null)
            originalColor = meshRenderer.material.color;

        if (abilityBar == null && player != null && player.isLocalPlayer())
            abilityBar = FindFirstObjectByType<AbilityBarUI>();

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] DelayedSpawn complete on {gameObject.name} | " +
                $"isServer={isServer} | isController={isController} | " +
                $"player={(player != null ? player.name : "NULL")} | " +
                $"bloodShotFirePoint={(bloodShotFirePoint != null ? bloodShotFirePoint.name : "NULL")} | " +
                $"abilityBar={(abilityBar != null ? abilityBar.name : "NULL")}"
            );
        }

        if (inputTester != null)
            inputTester.EnableInput();
    }

    private void Update()
    {
        if (bloodMeterDecayPerSecond > 0f && currentBloodMeter > 0f)
            ModifyBloodMeter(-bloodMeterDecayPerSecond * Time.deltaTime);

        if (bloodShotCooldownTimer > 0f)
            bloodShotCooldownTimer -= Time.deltaTime;

        if (quickPokeCooldownTimer > 0f)
            quickPokeCooldownTimer -= Time.deltaTime;

        if (globShotCooldownTimer > 0f)
            globShotCooldownTimer -= Time.deltaTime;

        if (ampUpCooldownTimer > 0f)
            ampUpCooldownTimer -= Time.deltaTime;

        UpdateAmpUp();
    }

    private void ModifyBloodMeter(float delta)
    {
        currentBloodMeter = Mathf.Clamp(currentBloodMeter + delta, 0f, maxBloodMeter);
    }

    public float GetBloodMeter01() => maxBloodMeter <= 0f ? 0f : currentBloodMeter / maxBloodMeter;

    private bool ValidateLocalAbilityCast(string abilityName, Transform requiredPoint = null)
    {
        if (player == null)
        {
            Debug.LogError($"[Mosquito] {abilityName} blocked - player is NULL.");
            return false;
        }

        if (!player.isLocalPlayer())
            return false;

        if (requiredPoint == null && abilityName != "Quick Poke" && abilityName != "Amp Up")
        {
            Debug.LogError($"[Mosquito] {abilityName} blocked - required point is NULL.");
            return false;
        }

        return true;
    }

    private void UpdateAbilityBarCooldown(int index, float cooldown)
    {
        if (abilityBar != null && player != null && player.isLocalPlayer())
            abilityBar.UseAbility(index, cooldown);
    }

    // Helper to get current cooldown based on Amp Up state
    private float GetCurrentBloodShotCooldown()
    {
        return isAmpUpActive ? ampUpBloodShotCooldownReduced : bloodShotCooldown;
    }

    private float GetCurrentQuickPokeCooldown()
    {
        return isAmpUpActive ? ampUpQuickPokeCooldownReduced : quickPokeCooldown;
    }

    private float GetCurrentGlobShotCooldown()
    {
        return isAmpUpActive ? ampUpGlobShotCooldownReduced : globShotCooldown;
    }

    // ========== BASIC ATTACK - BLOOD SHOT ==========
    public void CastBloodShot()
    {
        if (!ValidateLocalAbilityCast("Blood Shot", bloodShotFirePoint))
            return;

        if (bloodShotCooldownTimer > 0f)
        {
            if (enableBloodShotDebugLogs)
            {
                Debug.Log($"[Mosquito] Blood Shot blocked locally | remaining={Mathf.Max(0f, bloodShotCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableBloodShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Blood Shot requested locally | playerId={player.GetPlayerID()} | localTimer={bloodShotCooldownTimer:F2}s | isAmpUpActive={isAmpUpActive}");
        }

        float currentCooldown = GetCurrentBloodShotCooldown();

        if (startBloodShotCooldownLocallyOnInput)
            StartBloodShotCooldownClient(currentCooldown);

        PlayBloodShotAnimServerRpc();
        RequestBloodShotServerRpc(bloodShotFirePoint.position, bloodShotFirePoint.rotation);
    }

    private void StartBloodShotCooldownClient(float cooldown)
    {
        bloodShotCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(bloodShotAbilityBarIndex, cooldown);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Blood Shot local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestBloodShotServerRpc(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return;

        if (player == null)
        {
            Debug.LogError("[Mosquito] RequestBloodShotServerRpc failed - player is NULL on server.");
            return;
        }

        float serverTime = Time.time;
        float currentCooldown = GetCurrentBloodShotCooldown();
        float remaining = bloodShotNextAllowedTimeServer - serverTime;

        if (enableBloodShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Blood Shot request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={bloodShotNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s | isAmpUpActive={isAmpUpActive}");
        }

        if (serverTime < bloodShotNextAllowedTimeServer)
        {
            RejectBloodShotCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        bloodShotNextAllowedTimeServer = serverTime + currentCooldown;

        int damage = GetBasicAttackDamageWithBlood(bloodShotBaseDamage);
        ServerSpawnBloodShot(position, rotation, damage);
        SyncBloodShotCooldownClientRpc(currentCooldown);
    }

    [ObserversRpc]
    private void SyncBloodShotCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        bloodShotCooldownTimer = Mathf.Max(bloodShotCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(bloodShotAbilityBarIndex, cooldown);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Blood Shot cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectBloodShotCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        bloodShotCooldownTimer = Mathf.Max(bloodShotCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(bloodShotAbilityBarIndex, remainingCooldown);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Blood Shot rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ServerSpawnBloodShot(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Mosquito] ServerSpawnBloodShot - prefab={bloodShotProjectilePrefab}, firePoint={bloodShotFirePoint}, networkManager={networkManager}");

        if (bloodShotProjectilePrefab == null) { Debug.LogError("[Mosquito] bloodShotProjectilePrefab is NULL!"); return; }
        if (bloodShotFirePoint == null) { Debug.LogError("[Mosquito] bloodShotFirePoint is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Mosquito] networkManager is NULL!"); return; }

        GameObject proj = Instantiate(bloodShotProjectilePrefab, position, rotation);
        proj.GetComponent<BloodShotProjectile>().SpawnSetup(player, damage, player.transform.forward, bloodShotSpeed, null);
        NetworkManager.main.Spawn(proj);

        Debug.Log($"[Mosquito] Instantiated projectile: {proj.name}. PurrNet will auto-sync via NetworkBehaviour.");
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

        Debug.Log($"[Mosquito] OnBasicAttackHit before gain: {currentBloodMeter:F1}");

        ModifyBloodMeter(gain);

        Debug.Log($"[Mosquito] Blood gained: +{gain:F1} - current blood: {currentBloodMeter:F1}/{maxBloodMeter}");
    }

    // ========== QUICK POKE - ABILITY 2 ==========
    public bool TryQuickPoke()
    {
        if (!isController) return false;
        if (!ValidateLocalAbilityCast("Quick Poke"))
            return false;

        if (quickPokeCooldownTimer > 0f)
        {
            if (enableQuickPokeDebugLogs)
            {
                Debug.Log($"[Mosquito] Quick Poke blocked locally | remaining={Mathf.Max(0f, quickPokeCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return false;
        }

        if (enableQuickPokeDebugLogs)
        {
            Debug.Log($"[Mosquito] Quick Poke requested locally | playerId={player.GetPlayerID()} | localTimer={quickPokeCooldownTimer:F2}s | isAmpUpActive={isAmpUpActive}");
        }

        float currentCooldown = GetCurrentQuickPokeCooldown();

        if (startQuickPokeCooldownLocallyOnInput)
            StartQuickPokeCooldownClient(currentCooldown);

        PlayQuickPokeAnimServerRpc();
        RequestQuickPokeServerRpc();

        return true;
    }

    private void StartQuickPokeCooldownClient(float cooldown)
    {
        quickPokeCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(quickPokeAbilityBarIndex, cooldown);

        if (enableQuickPokeDebugLogs)
        {
            Debug.Log($"[Mosquito] Quick Poke local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestQuickPokeServerRpc()
    {
        if (!isServer)
            return;

        if (player == null)
        {
            Debug.LogError("[Mosquito] RequestQuickPokeServerRpc failed - player is NULL on server.");
            return;
        }

        float serverTime = Time.time;
        float currentCooldown = GetCurrentQuickPokeCooldown();
        float remaining = quickPokeNextAllowedTimeServer - serverTime;

        if (enableQuickPokeDebugLogs)
        {
            Debug.Log($"[Mosquito] Quick Poke request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={quickPokeNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s | isAmpUpActive={isAmpUpActive}");
        }

        if (serverTime < quickPokeNextAllowedTimeServer)
        {
            RejectQuickPokeCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        quickPokeNextAllowedTimeServer = serverTime + currentCooldown;
        ApplyQuickPoke();
        SyncQuickPokeCooldownClientRpc(currentCooldown);
    }

    [ObserversRpc]
    private void SyncQuickPokeCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        quickPokeCooldownTimer = Mathf.Max(quickPokeCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(quickPokeAbilityBarIndex, cooldown);

        if (enableQuickPokeDebugLogs)
        {
            Debug.Log($"[Mosquito] Quick Poke cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectQuickPokeCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        quickPokeCooldownTimer = Mathf.Max(quickPokeCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(quickPokeAbilityBarIndex, remainingCooldown);

        if (enableQuickPokeDebugLogs)
        {
            Debug.Log($"[Mosquito] Quick Poke rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
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

            Entity target = Entity.GetEntityFromCollider(hit);
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
        if (!ValidateLocalAbilityCast("Glob Shot", globFirePoint))
            return;

        if (globShotCooldownTimer > 0f)
        {
            if (enableGlobShotDebugLogs)
            {
                Debug.Log($"[Mosquito] Glob Shot blocked locally | remaining={Mathf.Max(0f, globShotCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        // Client-side blood check removed - server validates

        if (enableGlobShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Glob Shot requested locally | playerId={player.GetPlayerID()} | localTimer={globShotCooldownTimer:F2}s | isAmpUpActive={isAmpUpActive}");
        }

        float currentCooldown = GetCurrentGlobShotCooldown();

        if (startGlobShotCooldownLocallyOnInput)
            StartGlobShotCooldownClient(currentCooldown);

        PlayGlobShotAnimServerRpc();
        RequestGlobShotServerRpc(globFirePoint.position, globFirePoint.rotation);
    }

    private void StartGlobShotCooldownClient(float cooldown)
    {
        globShotCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(globShotAbilityBarIndex, cooldown);

        if (enableGlobShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Glob Shot local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestGlobShotServerRpc(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
            return;

        if (player == null)
        {
            Debug.LogError("[Mosquito] RequestGlobShotServerRpc failed - player is NULL on server.");
            return;
        }

        float serverTime = Time.time;
        float currentCooldown = GetCurrentGlobShotCooldown();
        float remaining = globShotNextAllowedTimeServer - serverTime;

        if (enableGlobShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Glob Shot request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={globShotNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s | isAmpUpActive={isAmpUpActive}");
        }

        if (serverTime < globShotNextAllowedTimeServer)
        {
            RejectGlobShotCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        // Server-side blood validation
        if (currentBloodMeter < globShotMinBloodThreshold)
        {
            Debug.Log($"[Mosquito] Glob Shot rejected on server - insufficient blood: {currentBloodMeter:F1}/{globShotMinBloodThreshold}");
            RejectGlobShotCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        globShotNextAllowedTimeServer = serverTime + currentCooldown;

        int damage = Mathf.RoundToInt(globBaseDamage);
        ServerSpawnGlobShot(position, rotation, damage);
        SyncGlobShotCooldownClientRpc(currentCooldown);
    }

    [ObserversRpc]
    private void SyncGlobShotCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        globShotCooldownTimer = Mathf.Max(globShotCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(globShotAbilityBarIndex, cooldown);

        if (enableGlobShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Glob Shot cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectGlobShotCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        globShotCooldownTimer = Mathf.Max(globShotCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(globShotAbilityBarIndex, remainingCooldown);

        if (enableGlobShotDebugLogs)
        {
            Debug.Log($"[Mosquito] Glob Shot rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ServerSpawnGlobShot(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Mosquito] ServerSpawnGlobShot - prefab={globProjectilePrefab}, firePoint={globFirePoint}, networkManager={networkManager}");

        if (globProjectilePrefab == null) { Debug.LogError("[Mosquito] globProjectilePrefab is NULL!"); return; }
        if (globFirePoint == null) { Debug.LogError("[Mosquito] globFirePoint is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Mosquito] networkManager is NULL!"); return; }

        GameObject proj = Instantiate(globProjectilePrefab, position, rotation);
        proj.GetComponent<GlobProjectile>().SpawnSetup(player, damage, player.transform.forward, globBaseSpeed, null);
        NetworkManager.main.Spawn(proj);

        Debug.Log($"[Mosquito] Instantiated glob projectile: {proj.name}. PurrNet will auto-sync via NetworkBehaviour.");
    }

    // ========== AMP UP - ULTIMATE ==========
    public void ActivateAmpUp()
    {
        if (!isController) return;
        if (!ValidateLocalAbilityCast("Amp Up"))
            return;

        if (ampUpCooldownTimer > 0f)
        {
            if (enableAmpUpDebugLogs)
            {
                Debug.Log($"[Mosquito] Amp Up blocked locally by cooldown | remaining={Mathf.Max(0f, ampUpCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (ampUpTimer > 0f)
        {
            Debug.Log("[Mosquito] Amp Up blocked - already active.");
            return;
        }

        if (enableAmpUpDebugLogs)
        {
            Debug.Log($"[Mosquito] Amp Up requested locally | playerId={player.GetPlayerID()} | localTimer={ampUpCooldownTimer:F2}s");
        }

        if (startAmpUpCooldownLocallyOnInput)
            StartAmpUpCooldownClient(ampUpCooldown);

        PlayAmpUpAnimServerRpc();
        RequestAmpUpServerRpc();
    }

    private void StartAmpUpCooldownClient(float cooldown)
    {
        ampUpCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(ampUpAbilityBarIndex, cooldown);

        if (enableAmpUpDebugLogs)
        {
            Debug.Log($"[Mosquito] Amp Up local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void RequestAmpUpServerRpc()
    {
        if (!isServer)
            return;

        if (player == null)
        {
            Debug.LogError("[Mosquito] RequestAmpUpServerRpc failed - player is NULL on server.");
            return;
        }

        float serverTime = Time.time;
        float remaining = ampUpNextAllowedTimeServer - serverTime;

        if (enableAmpUpDebugLogs)
        {
            Debug.Log($"[Mosquito] Amp Up request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={ampUpNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < ampUpNextAllowedTimeServer)
        {
            RejectAmpUpCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        if (ampUpTimer > 0f)
        {
            Debug.Log("[Mosquito] Amp Up blocked on server - already active.");
            return;
        }

        ampUpNextAllowedTimeServer = serverTime + ampUpCooldown;
        ampUpTimer = ampUpDuration;
        isAmpUpActive = true;

        ApplyAmpUp();

        // Sync Amp Up state to all clients
        SyncAmpUpStateClientRpc(true, ampUpTimer);
        SyncAmpUpCooldownClientRpc(ampUpCooldown);
    }

    [ObserversRpc]
    private void SyncAmpUpStateClientRpc(bool active, float remainingTime)
    {
        isAmpUpActive = active;

        if (active)
        {
            ampUpTimer = remainingTime;

            // Update ability bar cooldowns to show reduced values
            if (player != null && player.isLocalPlayer())
            {
                // You might want to refresh ability cooldown displays here
                Debug.Log($"[Mosquito] Amp Up activated on client - cooldowns reduced by {ampUpCooldownReductionMultiplier * 100}%");
            }
        }

        if (enableAmpUpDebugLogs)
        {
            Debug.Log($"[Mosquito] Amp Up state synced | active={active} | remaining={remainingTime:F2}s");
        }
    }

    [ObserversRpc]
    private void SyncAmpUpCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        ampUpCooldownTimer = Mathf.Max(ampUpCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(ampUpAbilityBarIndex, cooldown);

        if (enableAmpUpDebugLogs)
        {
            Debug.Log($"[Mosquito] Amp Up cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectAmpUpCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        ampUpCooldownTimer = Mathf.Max(ampUpCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(ampUpAbilityBarIndex, remainingCooldown);

        if (enableAmpUpDebugLogs)
        {
            Debug.Log($"[Mosquito] Amp Up rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ApplyAmpUp()
    {
        Debug.Log($"[Mosquito] Amp Up activated! Duration={ampUpDuration}s, MoveMult={ampUpInitialMoveMult}, AttackMult={ampUpInitialAttackSpeedMult}, Cooldown Reduction={ampUpCooldownReductionMultiplier * 100}%");

        player.ModifyMoveSpeedMultiplier(ampUpInitialMoveMult, ampUpDuration);
        player.ModifyAttackPowerForSeconds(ampUpInitialAttackSpeedMult, ampUpDuration);

        SetAmpUpColorRpc(true);
    }

    private void UpdateAmpUp()
    {
        if (ampUpTimer <= 0f)
        {
            if (isAmpUpActive)
            {
                // Amp Up expired
                isAmpUpActive = false;
                if (isServer)
                {
                    SetAmpUpColorRpc(false);
                    SyncAmpUpStateClientRpc(false, 0f);
                }
                Debug.Log("[Mosquito] Amp Up expired - cooldowns restored to normal.");
            }
            return;
        }

        ampUpTimer -= Time.deltaTime;

        if (ampUpTimer <= 0f && isAmpUpActive)
        {
            isAmpUpActive = false;
            if (isServer)
            {
                SetAmpUpColorRpc(false);
                SyncAmpUpStateClientRpc(false, 0f);
            }
            Debug.Log("[Mosquito] Amp Up expired - cooldowns restored to normal.");
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

        if (meshRenderer != null)
            meshRenderer.material.color = active ? ampUpColor : originalColor;

        Debug.Log($"[Mosquito] Amp Up color set to {(active ? "active" : "original")}.");
    }

    // ========== ANIMATOR METHODS ==========
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

    // ========== GIZMOS FOR VISUALIZATION ==========
    private void OnDrawGizmosSelected()
    {
        if (quickPokeOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(quickPokeOrigin.position, quickPokeRange);
        }
    }
}