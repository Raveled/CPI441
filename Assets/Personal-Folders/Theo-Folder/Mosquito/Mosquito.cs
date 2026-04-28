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
    [SerializeField] private float globShotCooldown = 5f;
    private float globShotCooldownTimer = 0f;

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
    [SerializeField] public MosquitoInputTester inputTester;

    // Basic attack cooldown state
    private float bloodShotCooldownTimer = 0f;
    private float bloodShotNextAllowedTimeServer = 0f;

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

        if (globShotCooldownTimer > 0f)
            globShotCooldownTimer -= Time.deltaTime;

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
        if (player == null)
        {
            Debug.LogError("[Mosquito] CastBloodShot blocked - player is NULL.");
            return;
        }

        if (!player.isLocalPlayer())
            return;

        if (bloodShotFirePoint == null)
        {
            Debug.LogError("[Mosquito] CastBloodShot blocked - bloodShotFirePoint is NULL.");
            return;
        }

        if (!CanCastBloodShotClient())
        {
            if (enableBloodShotDebugLogs)
            {
                Debug.Log(
                    $"[Mosquito] Blood Shot blocked locally on {gameObject.name} | " +
                    $"localRemaining={Mathf.Max(0f, bloodShotCooldownTimer):F2}s | " +
                    $"playerId={player.GetPlayerID()}"
                );
            }
            return;
        }

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] CastBloodShot local request | " +
                $"object={gameObject.name} | playerId={player.GetPlayerID()} | " +
                $"isLocal={player.isLocalPlayer()} | localTimer={bloodShotCooldownTimer:F2}s"
            );
        }

        if (startBloodShotCooldownLocallyOnInput)
            StartBloodShotCooldownClient(bloodShotCooldown);

        PlayBloodShotAnimServerRpc();
        RequestBloodShotServerRpc(bloodShotFirePoint.position, bloodShotFirePoint.rotation);
    }

    private bool CanCastBloodShotClient()
    {
        return bloodShotCooldownTimer <= 0f;
    }

    private void StartBloodShotCooldownClient(float cooldown)
    {
        bloodShotCooldownTimer = cooldown;

        if (abilityBar != null && player != null && player.isLocalPlayer())
            abilityBar.UseAbility(bloodShotAbilityBarIndex, cooldown);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] Started local Blood Shot cooldown | " +
                $"cooldown={cooldown:F2}s | playerId={(player != null ? player.GetPlayerID().ToString() : "NULL")}"
            );
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
        float remaining = bloodShotNextAllowedTimeServer - serverTime;

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] Blood Shot request arrived on server | " +
                $"playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | " +
                $"nextAllowed={bloodShotNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s"
            );
        }

        if (serverTime < bloodShotNextAllowedTimeServer)
        {
            if (enableBloodShotDebugLogs)
            {
                Debug.Log(
                    $"[Mosquito] Blood Shot rejected on server | " +
                    $"playerId={player.GetPlayerID()} | remaining={Mathf.Max(0f, remaining):F2}s"
                );
            }

            RejectBloodShotCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        bloodShotNextAllowedTimeServer = serverTime + bloodShotCooldown;

        int damage = GetBasicAttackDamageWithBlood(bloodShotBaseDamage);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] Blood Shot approved on server | " +
                $"playerId={player.GetPlayerID()} | damage={damage} | " +
                $"nextAllowed={bloodShotNextAllowedTimeServer:F2}"
            );
        }

        ServerSpawnBloodShot(position, rotation, damage);
        SyncBloodShotCooldownClientRpc(bloodShotCooldown);
    }

    [ObserversRpc]
    private void SyncBloodShotCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        bloodShotCooldownTimer = Mathf.Max(bloodShotCooldownTimer, cooldown);

        if (abilityBar != null)
            abilityBar.UseAbility(bloodShotAbilityBarIndex, cooldown);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] Blood Shot cooldown synced to local player | " +
                $"cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}"
            );
        }
    }

    [ObserversRpc]
    private void RejectBloodShotCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        bloodShotCooldownTimer = Mathf.Max(bloodShotCooldownTimer, remainingCooldown);

        if (abilityBar != null && remainingCooldown > 0f)
            abilityBar.UseAbility(bloodShotAbilityBarIndex, remainingCooldown);

        if (enableBloodShotDebugLogs)
        {
            Debug.Log(
                $"[Mosquito] Blood Shot rejected sync received by local player | " +
                $"remainingCooldown={remainingCooldown:F2}s | playerId={player.GetPlayerID()}"
            );
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnBloodShotRpc(Vector3 position, Quaternion rotation, int damage)
    {
        if (!isServer) return;

        Debug.Log($"[Mosquito] ServerSpawnBloodShotRpc received on server. damage={damage} player id = {player.GetPlayerID()}");
        ServerSpawnBloodShot(position, rotation, damage);
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
        if (quickPokeCooldownTimer > 0f || player == null)
        {
            Debug.Log($"[Mosquito] Quick Poke blocked - cooldown: {quickPokeCooldownTimer:F2}s remaining");
            return false;
        }

        quickPokeCooldownTimer = quickPokeCooldown;

        PlayQuickPokeAnimServerRpc();

        if (isServer)
            ApplyQuickPoke();
        else
            ApplyQuickPokeServerRpc();

        if (abilityBar != null && player != null && player.isLocalPlayer())
            abilityBar.UseAbility(1, quickPokeCooldown);

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
        if (!player.isLocalPlayer()) return;
        if (globShotCooldownTimer > 0f) return;

        Debug.Log($"[Mosquito] CastGlobShot on {gameObject.name} | Player ID: {player.GetPlayerID()} | Player is Local: {player.isLocalPlayer()}");

        int damage = Mathf.RoundToInt(globBaseDamage);
        PlayGlobShotAnim();

        Debug.Log("[Mosquito] Sending GlobShot ServerRpc.");
        ServerSpawnGlobShotRpc(globFirePoint.position, globFirePoint.rotation, damage);

        globShotCooldownTimer = globShotCooldown;

        if (abilityBar != null)
            abilityBar.UseAbility(2, globShotCooldown);
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnGlobShotRpc(Vector3 position, Quaternion rotation, int damage)
    {
        if (!isServer) return;

        Debug.Log($"[Mosquito] ServerSpawnGlobShotRpc received on server. damage={damage} player id={player.GetPlayerID()}");
        ServerSpawnGlobShot(position, rotation, damage);
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
        if (ampUpTimer > 0f)
        {
            Debug.Log("[Mosquito] Amp Up blocked - already active.");
            return;
        }

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