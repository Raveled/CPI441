using System.Collections;
using UnityEngine;
using PurrNet;
using PurrNet.Prediction;

public class Butterfly : NetworkBehaviour
{
    [SerializeField] public Player player;
    [SerializeField] public ButterflyInputTester inputTester;

    [Header("UI cooldown hook up")]
    [SerializeField] private AbilityBarUI abilityBar;

    [Header("Basic Attack - Wind Burst")]
    [SerializeField] private GameObject windBurstProjectilePrefab;
    [SerializeField] private Transform windBurstFirePoint;
    [SerializeField] private int windBurstBaseDamage = 4;
    [SerializeField] private float windBurstSpeed = 10f;
    [SerializeField] private float windBurstRange = 6f;
    [SerializeField] private float windBurstCooldown = 0.3f;
    [SerializeField] private int windBurstAbilityBarIndex = -1;
    [SerializeField] private bool startWindBurstCooldownLocallyOnInput = true;
    [SerializeField] private bool enableWindBurstDebugLogs = true;

    [Header("Dust Wave - Ability 1")]
    [SerializeField] private GameObject dustWavePrefab;
    [SerializeField] private Transform dustWaveOrigin;
    [SerializeField] private int dustWaveBaseDamage = 3;
    [SerializeField] private float dustWaveRadius = 4f;
    [SerializeField] private float dustWaveCooldown = 5f;
    [SerializeField] private int dustWaveAbilityBarIndex = 0;
    [SerializeField] private bool startDustWaveCooldownLocallyOnInput = true;
    [SerializeField] private bool enableDustWaveDebugLogs = true;

    [Header("Dazzling Wave - Ability 2")]
    [SerializeField] private GameObject dazzlingWavePrefab;
    [SerializeField] private Transform dazzlingWaveOrigin;
    [SerializeField] private int dazzlingWaveBaseDamage = 2;
    [SerializeField] private float dazzlingWaveRadius = 4f;
    [SerializeField] private int dazzlingWaveHealAmount = 10;
    [SerializeField] private float dazzlingWaveCooldown = 6f;
    [SerializeField] private bool dazzlingWaveUpgradeReducedDamage = false;
    [SerializeField] private float dazzlingWaveDamageReductionMultiplier = 0.7f;
    [SerializeField] private int dazzlingWaveAbilityBarIndex = 1;
    [SerializeField] private bool startDazzlingWaveCooldownLocallyOnInput = true;
    [SerializeField] private bool enableDazzlingWaveDebugLogs = true;

    [Header("Fly - Ability 3")]
    [SerializeField] private float flyDashDistance = 8f;
    [SerializeField] private float flyDashDuration = 0.35f;
    [SerializeField] private float flyCooldown = 7f;
    [SerializeField] private bool flyUpgradeTwoCharges = false;
    [SerializeField] private int flyMaxChargesBase = 1;
    [SerializeField] private int flyMaxChargesUpgraded = 2;
    [SerializeField] private int flyAbilityBarIndex = 2;
    [SerializeField] private bool startFlyCooldownLocallyOnInput = true;
    [SerializeField] private bool enableFlyDebugLogs = true;

    [Header("Tornado - Ultimate")]
    [SerializeField] private GameObject tornadoPrefab;
    [SerializeField] private float tornadoRadius = 5f;
    [SerializeField] private float tornadoDuration = 4f;
    [SerializeField] private int tornadoBaseDamagePerTick = 2;
    [SerializeField] private float tornadoTickInterval = 0.5f;
    [SerializeField] private float tornadoGroupForce = 10f;
    [SerializeField] private float tornadoCooldown = 30f;
    [SerializeField] private int tornadoAbilityBarIndex = 3;
    [SerializeField] private bool startTornadoCooldownLocallyOnInput = true;
    [SerializeField] private bool enableTornadoDebugLogs = true;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    // Local cooldown timers
    private float windBurstCooldownTimer = 0f;
    private float dustWaveCooldownTimer = 0f;
    private float dazzlingWaveCooldownTimer = 0f;
    private float flyCooldownTimer = 0f;
    private float tornadoCooldownTimer = 0f;

    // Server authoritative cooldown times
    private float windBurstNextAllowedTimeServer = 0f;
    private float dustWaveNextAllowedTimeServer = 0f;
    private float dazzlingWaveNextAllowedTimeServer = 0f;
    private float flyNextAllowedTimeServer = 0f;
    private float tornadoNextAllowedTimeServer = 0f;

    private int flyCurrentCharges = 0;
    private bool isFlying = false;
    private Vector3 currentFlyDirection = Vector3.forward;
    private PredictedPlayerMovement predictedMovement;

    protected override void OnSpawned(bool asServer)
    {
        StartCoroutine(DelayedSpawn(asServer));
    }

    private IEnumerator DelayedSpawn(bool asServer)
    {
        yield return new WaitForSeconds(0.1f);

        base.OnSpawned();

        if (player == null)
            player = GetComponent<Player>();

        predictedMovement = GetComponentInParent<PredictedPlayerMovement>();
        flyCurrentCharges = GetFlyMaxCharges();

        GameObject parentObject = transform.parent != null ? transform.parent.gameObject : gameObject;

        if (windBurstFirePoint == null)
            windBurstFirePoint = parentObject.GetComponent<PredictedPlayerMovement>().firingPoint.transform;

        if (abilityBar == null && player != null && player.isLocalPlayer())
            abilityBar = FindFirstObjectByType<AbilityBarUI>();

        if (dustWaveOrigin == null)
            dustWaveOrigin = windBurstFirePoint;

        if (dazzlingWaveOrigin == null)
            dazzlingWaveOrigin = windBurstFirePoint;

        if (animator == null)
            animator = parentObject.GetComponentInChildren<Animator>();

        if (inputTester != null)
            inputTester.enabled = true;
    }

    private void Update()
    {
        // Update local cooldown timers
        if (windBurstCooldownTimer > 0f)
            windBurstCooldownTimer -= Time.deltaTime;

        if (dustWaveCooldownTimer > 0f)
            dustWaveCooldownTimer -= Time.deltaTime;

        if (dazzlingWaveCooldownTimer > 0f)
            dazzlingWaveCooldownTimer -= Time.deltaTime;

        if (flyCooldownTimer > 0f)
            flyCooldownTimer -= Time.deltaTime;

        if (tornadoCooldownTimer > 0f)
            tornadoCooldownTimer -= Time.deltaTime;

        if (isServer)
            HandleFlyChargeRecharge();
    }

    private void UpdateAbilityBarCooldown(int index, float cooldown)
    {
        if (abilityBar == null || player == null || !player.isLocalPlayer())
            return;

        if (index < 0)
            return;

        abilityBar.UseAbility(index, cooldown);
    }
    private float GetFinalCooldown(float baseCooldown)
    {
        return player != null ? player.GetModifiedAbilityCooldown(baseCooldown) : baseCooldown;
    }
    #region Basic Attack - Wind Burst

    public void CastWindBurst()
    {
        if (player == null) return;
        if (!player.isLocalPlayer()) return;
        if (ShopUI.IsAnyOpen)
            return;
        if (windBurstCooldownTimer > 0f)
        {
            if (enableWindBurstDebugLogs)
            {
                Debug.Log($"[Butterfly] Wind Burst blocked locally | remaining={Mathf.Max(0f, windBurstCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableWindBurstDebugLogs)
        {
            Debug.Log($"[Butterfly] Wind Burst requested locally | playerId={player.GetPlayerID()} | localTimer={windBurstCooldownTimer:F2}s");
        }

        if (startWindBurstCooldownLocallyOnInput)
            StartWindBurstCooldownClient(GetFinalCooldown(windBurstCooldown));

        // Trigger animation locally
        if (animator != null)
            animator.SetTrigger("WindBurst");

        Debug.Log($"[Butterfly] CastWindBurst on {gameObject.name} | Player ID: {player.GetPlayerID()} | Player is Local: {player.isLocalPlayer()}");

        int damage = windBurstBaseDamage;

        Debug.Log("[Butterfly] Sending WindBurst ServerRpc.");
        ServerSpawnWindBurstRpc(windBurstFirePoint.position, windBurstFirePoint.rotation, damage);
    }

    private void StartWindBurstCooldownClient(float cooldown)
    {
        Debug.Log($"[Butterfly] Wind Burst UI index = {windBurstAbilityBarIndex}");

        windBurstCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(windBurstAbilityBarIndex, cooldown);

        if (enableWindBurstDebugLogs)
        {
            Debug.Log($"[Butterfly] Wind Burst local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnWindBurstRpc(Vector3 position, Quaternion rotation, int damage)
    {
        if (!isServer) return;

        float serverTime = Time.time;
        float remaining = windBurstNextAllowedTimeServer - serverTime;

        if (enableWindBurstDebugLogs)
        {
            Debug.Log($"[Butterfly] Wind Burst request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={windBurstNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < windBurstNextAllowedTimeServer)
        {
            RejectWindBurstCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        float finalCooldown = GetFinalCooldown(windBurstCooldown);
        windBurstNextAllowedTimeServer = serverTime + finalCooldown;

        // Play animation on all observers
        PlayAnimationObserversRpc("WindBurst");

        Debug.Log($"[Butterfly] ServerSpawnWindBurstRpc received on server. damage={damage} player id={player.GetPlayerID()}");
        ServerSpawnWindBurst(position, rotation, damage);
        SyncWindBurstCooldownClientRpc(finalCooldown);

    }

    [ObserversRpc]
    private void SyncWindBurstCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        windBurstCooldownTimer = Mathf.Max(windBurstCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(windBurstAbilityBarIndex, cooldown);

        if (enableWindBurstDebugLogs)
        {
            Debug.Log($"[Butterfly] Wind Burst cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectWindBurstCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        windBurstCooldownTimer = Mathf.Max(windBurstCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(windBurstAbilityBarIndex, remainingCooldown);

        if (enableWindBurstDebugLogs)
        {
            Debug.Log($"[Butterfly] Wind Burst rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ServerSpawnWindBurst(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Butterfly] ServerSpawnWindBurst - prefab={windBurstProjectilePrefab}, firePoint={windBurstFirePoint}, networkManager={networkManager}");

        if (windBurstProjectilePrefab == null) { Debug.LogError("[Butterfly] windBurstProjectilePrefab is NULL!"); return; }
        if (windBurstFirePoint == null) { Debug.LogError("[Butterfly] windBurstFirePoint is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Butterfly] networkManager is NULL!"); return; }

        GameObject proj = Instantiate(windBurstProjectilePrefab, position, rotation);

        WindBurstProjectile projectile = proj.GetComponent<WindBurstProjectile>();
        if (projectile == null)
        {
            Debug.LogError("[Butterfly] Spawned projectile is missing WindBurstProjectile component!");
            Destroy(proj);
            return;
        }

        projectile.SpawnSetup(player, damage, player.transform.forward, windBurstSpeed, null);

        Debug.Log(
            $"[Butterfly][WindBurst][Server] SpawnSetup complete | " +
            $"owner={player.name} | ownerId={player.GetNetworkID(true)} | " +
            $"damage={damage} | dir={player.transform.forward} | speed={windBurstSpeed} | " +
            $"spawnPos={position}"
        );

        NetworkManager.main.Spawn(proj);

        Debug.Log($"[Butterfly] Instantiated projectile: {proj.name}. PurrNet will auto-sync via NetworkBehaviour.");
    }

    #endregion

    #region Ability 1 - Dust Wave

    public void CastDustWave()
    {
        float finalCooldown = GetFinalCooldown(dustWaveCooldown);
        if (player == null) return;
        if (!player.isLocalPlayer()) return;

        if (dustWaveCooldownTimer > 0f)
        {
            if (enableDustWaveDebugLogs)
            {
                Debug.Log($"[Butterfly] Dust Wave blocked locally | remaining={Mathf.Max(0f, dustWaveCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableDustWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dust Wave requested locally | playerId={player.GetPlayerID()} | localTimer={dustWaveCooldownTimer:F2}s");
        }

        if (startDustWaveCooldownLocallyOnInput)
            StartDustWaveCooldownClient(GetFinalCooldown(dustWaveCooldown));

        if (animator != null)
            animator.SetTrigger("DustStorm");

        int damage = dustWaveBaseDamage;

        Debug.Log($"[Butterfly] CastDustWave on {gameObject.name} | Player ID: {player.GetPlayerID()} | Player is Local: {player.isLocalPlayer()}");
        Debug.Log("[Butterfly] Sending DustWave ServerRpc.");

        ServerSpawnDustWaveRpc(dustWaveOrigin.position, dustWaveOrigin.rotation, damage);
    }

    private void StartDustWaveCooldownClient(float cooldown)
    {
        dustWaveCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(dustWaveAbilityBarIndex, cooldown);

        if (enableDustWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dust Wave local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnDustWaveRpc(Vector3 position, Quaternion rotation, int damage)
    {
        if (!isServer) return;
        float finalCooldown = GetFinalCooldown(dustWaveCooldown);
        float serverTime = Time.time;
        float remaining = dustWaveNextAllowedTimeServer - serverTime;

        if (enableDustWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dust Wave request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={dustWaveNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < dustWaveNextAllowedTimeServer)
        {
            RejectDustWaveCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        dustWaveNextAllowedTimeServer = serverTime + finalCooldown;

        // Play animation on all observers
        PlayAnimationObserversRpc("DustStorm");

        Debug.Log($"[Butterfly] ServerSpawnDustWaveRpc received on server. damage={damage} player id={player.GetPlayerID()}");
        ServerSpawnDustWave(position, rotation, damage);
        SyncDustWaveCooldownClientRpc(finalCooldown);
    }

    [ObserversRpc]
    private void SyncDustWaveCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        dustWaveCooldownTimer = Mathf.Max(dustWaveCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(dustWaveAbilityBarIndex, cooldown);

        if (enableDustWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dust Wave cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectDustWaveCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        dustWaveCooldownTimer = Mathf.Max(dustWaveCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(dustWaveAbilityBarIndex, remainingCooldown);

        if (enableDustWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dust Wave rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ServerSpawnDustWave(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Butterfly] ServerSpawnDustWave - prefab={dustWavePrefab}, origin={dustWaveOrigin}, networkManager={networkManager}");

        if (dustWavePrefab == null) { Debug.LogError("[Butterfly] dustWavePrefab is NULL!"); return; }
        if (dustWaveOrigin == null) { Debug.LogError("[Butterfly] dustWaveOrigin is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Butterfly] networkManager is NULL!"); return; }

        GameObject proj = Instantiate(dustWavePrefab, position, rotation);

        DustWaveMovement projectile = proj.GetComponent<DustWaveMovement>();
        if (projectile == null)
        {
            Debug.LogError("[Butterfly] Spawned projectile is missing DustWaveMovement component!");
            Destroy(proj);
            return;
        }

        projectile.SpawnSetup(player, damage, player.transform.forward, windBurstSpeed, null);

        Debug.Log(
            $"[Butterfly][DustWave][Server] SpawnSetup complete | " +
            $"owner={player.name} | ownerId={player.GetNetworkID(true)} | " +
            $"damage={damage} | dir={player.transform.forward} | speed={windBurstSpeed} | " +
            $"spawnPos={position}"
        );

        NetworkManager.main.Spawn(proj);

        Debug.Log($"[Butterfly] Instantiated dust wave projectile: {proj.name}. PurrNet will auto-sync via NetworkBehaviour.");
    }

    #endregion

    #region Ability 2 - Dazzling Wave

    public void CastDazzlingWave()
    {
        if (player == null) return;
        if (!player.isLocalPlayer()) return;

        if (dazzlingWaveCooldownTimer > 0f)
        {
            if (enableDazzlingWaveDebugLogs)
            {
                Debug.Log($"[Butterfly] Dazzling Wave blocked locally | remaining={Mathf.Max(0f, dazzlingWaveCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableDazzlingWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dazzling Wave requested locally | playerId={player.GetPlayerID()} | localTimer={dazzlingWaveCooldownTimer:F2}s");
        }

        if (startDazzlingWaveCooldownLocallyOnInput)
            StartDazzlingWaveCooldownClient(GetFinalCooldown(dazzlingWaveCooldown));

        if (animator != null)
            animator.SetTrigger("DazzlingWave");

        int damage = dazzlingWaveBaseDamage;

        Debug.Log($"[Butterfly] CastDazzlingWave on {gameObject.name} | Player ID: {player.GetPlayerID()} | Player is Local: {player.isLocalPlayer()}");
        Debug.Log("[Butterfly] Sending DazzlingWave ServerRpc.");

        ServerSpawnDazzlingWaveRpc(dazzlingWaveOrigin.position, dazzlingWaveOrigin.rotation, damage);
    }

    private void StartDazzlingWaveCooldownClient(float cooldown)
    {
        dazzlingWaveCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(dazzlingWaveAbilityBarIndex, cooldown);

        if (enableDazzlingWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dazzling Wave local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnDazzlingWaveRpc(Vector3 position, Quaternion rotation, int damage)
    {
        if (!isServer) return;
        float finalCooldown = GetFinalCooldown(dazzlingWaveCooldown);
        float serverTime = Time.time;
        float remaining = dazzlingWaveNextAllowedTimeServer - serverTime;

        if (enableDazzlingWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dazzling Wave request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={dazzlingWaveNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < dazzlingWaveNextAllowedTimeServer)
        {
            RejectDazzlingWaveCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        dazzlingWaveNextAllowedTimeServer = serverTime + finalCooldown;

        // Play animation on all observers
        PlayAnimationObserversRpc("DazzlingWave");

        Debug.Log($"[Butterfly] ServerSpawnDazzlingWaveRpc received on server. damage={damage} player id={player.GetPlayerID()}");
        ServerSpawnDazzlingWave(position, rotation, damage);
        SyncDazzlingWaveCooldownClientRpc(finalCooldown);
    }

    [ObserversRpc]
    private void SyncDazzlingWaveCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        dazzlingWaveCooldownTimer = Mathf.Max(dazzlingWaveCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(dazzlingWaveAbilityBarIndex, cooldown);

        if (enableDazzlingWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dazzling Wave cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectDazzlingWaveCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        dazzlingWaveCooldownTimer = Mathf.Max(dazzlingWaveCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(dazzlingWaveAbilityBarIndex, remainingCooldown);

        if (enableDazzlingWaveDebugLogs)
        {
            Debug.Log($"[Butterfly] Dazzling Wave rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ServerSpawnDazzlingWave(Vector3 position, Quaternion rotation, int damage)
    {
        Debug.Log($"[Butterfly] ServerSpawnDazzlingWave - prefab={dazzlingWavePrefab}, origin={dazzlingWaveOrigin}, networkManager={networkManager}");

        if (dazzlingWavePrefab == null) { Debug.LogError("[Butterfly] dazzlingWavePrefab is NULL!"); return; }
        if (dazzlingWaveOrigin == null) { Debug.LogError("[Butterfly] dazzlingWaveOrigin is NULL!"); return; }
        if (networkManager == null) { Debug.LogError("[Butterfly] networkManager is NULL!"); return; }

        GameObject proj = Instantiate(dazzlingWavePrefab, position, rotation);

        DazzlingWaveProjectile projectile = proj.GetComponent<DazzlingWaveProjectile>();
        if (projectile == null)
        {
            Debug.LogError("[Butterfly] Spawned projectile is missing DazzlingWaveProjectile component!");
            Destroy(proj);
            return;
        }

        projectile.SpawnSetup(player, damage, player.transform.forward, windBurstSpeed, null);

        Debug.Log(
            $"[Butterfly][DazzlingWave][Server] SpawnSetup complete | " +
            $"owner={player.name} | ownerId={player.GetNetworkID(true)} | " +
            $"damage={damage} | dir={player.transform.forward} | speed={windBurstSpeed} | " +
            $"spawnPos={position}"
        );

        NetworkManager.main.Spawn(proj);

        Debug.Log($"[Butterfly] Instantiated dazzling wave projectile: {proj.name}. PurrNet will auto-sync via NetworkBehaviour.");
    }

    #endregion

    #region Ability 3 - Fly

    public void StartFly(Vector3 direction)
    {
        if (player == null) return;
        if (!player.isLocalPlayer()) return;

        if (isFlying)
        {
            Debug.Log("[Butterfly] Fly blocked - already flying.");
            return;
        }

        if (flyCurrentCharges <= 0)
        {
            Debug.Log("[Butterfly] Fly blocked - no charges.");
            return;
        }

        if (flyCooldownTimer > 0f)
        {
            if (enableFlyDebugLogs)
            {
                Debug.Log($"[Butterfly] Fly blocked locally by cooldown | remaining={Mathf.Max(0f, flyCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableFlyDebugLogs)
        {
            Debug.Log($"[Butterfly] Fly requested locally | playerId={player.GetPlayerID()} | localTimer={flyCooldownTimer:F2}s");
        }

        if (startFlyCooldownLocallyOnInput)
            StartFlyCooldownClient(GetFinalCooldown(flyCooldown));

        if (animator != null)
            animator.SetTrigger("Fly");

        Vector3 finalDirection = direction.normalized;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = transform.forward;

        Debug.Log($"[Butterfly] StartFly requested by local player. dir={finalDirection}");
        ServerStartFlyRpc(finalDirection);
    }

    private void StartFlyCooldownClient(float cooldown)
    {
        flyCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(flyAbilityBarIndex, cooldown);

        if (enableFlyDebugLogs)
        {
            Debug.Log($"[Butterfly] Fly local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerStartFlyRpc(Vector3 direction)
    {
        if (!isServer) return;
        float finalCooldown = GetFinalCooldown(flyCooldown);
        float serverTime = Time.time;
        float remaining = flyNextAllowedTimeServer - serverTime;

        if (enableFlyDebugLogs)
        {
            Debug.Log($"[Butterfly] Fly request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={flyNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < flyNextAllowedTimeServer)
        {
            RejectFlyCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        if (isFlying)
        {
            Debug.Log("[Butterfly] ServerStartFlyRpc blocked - already flying.");
            return;
        }

        if (flyCurrentCharges <= 0)
        {
            Debug.Log("[Butterfly] ServerStartFlyRpc blocked - no charges.");
            return;
        }

        flyNextAllowedTimeServer = serverTime + finalCooldown;

        // Play animation on all observers
        PlayAnimationObserversRpc("Fly");

        Vector3 finalDirection = direction.normalized;
        if (finalDirection.sqrMagnitude <= 0.001f)
            finalDirection = transform.forward;

        flyCurrentCharges--;
        flyCooldownTimer = flyCooldown;
        isFlying = true;
        currentFlyDirection = finalDirection;

        Debug.Log($"[Butterfly] Server starting Fly. Remaining charges={flyCurrentCharges}");

        BeginFlyObserversRpc(finalDirection);
        SyncFlyCooldownClientRpc(finalCooldown);

        if (predictedMovement != null)
            predictedMovement.StartButterflyFly(finalDirection, flyDashDistance, flyDashDuration);
    }

    [ObserversRpc]
    private void SyncFlyCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        flyCooldownTimer = Mathf.Max(flyCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(flyAbilityBarIndex, cooldown);

        if (enableFlyDebugLogs)
        {
            Debug.Log($"[Butterfly] Fly cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectFlyCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        flyCooldownTimer = Mathf.Max(flyCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(flyAbilityBarIndex, remainingCooldown);

        if (enableFlyDebugLogs)
        {
            Debug.Log($"[Butterfly] Fly rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void BeginFlyObserversRpc(Vector3 direction)
    {
        isFlying = true;
        currentFlyDirection = direction.normalized;

        if (currentFlyDirection.sqrMagnitude <= 0.001f)
            currentFlyDirection = transform.forward;

        if (!isServer && predictedMovement != null)
            predictedMovement.StartButterflyFly(currentFlyDirection, flyDashDistance, flyDashDuration);

        Debug.Log($"[Butterfly] Fly started on observer. dir={currentFlyDirection}");
    }

    public void CancelFly()
    {
        if (player == null) return;
        if (!player.isLocalPlayer()) return;
        if (!isFlying) return;

        Debug.Log("[Butterfly] CancelFly requested.");
        ServerCancelFlyRpc();
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerCancelFlyRpc()
    {
        if (!isServer) return;
        if (!isFlying) return;

        Debug.Log("[Butterfly] Server cancelling Fly.");
        EndFlyServer();
    }

    public void NotifyFlyEndedFromMovement()
    {
        if (!isServer) return;
        if (!isFlying) return;

        EndFlyServer();
    }

    private void EndFlyServer()
    {
        isFlying = false;
        EndFlyObserversRpc();
    }

    [ObserversRpc]
    private void EndFlyObserversRpc()
    {
        isFlying = false;

        if (predictedMovement != null)
            predictedMovement.StopButterflyFly();

        Debug.Log("[Butterfly] Fly ended.");
    }

    private int GetFlyMaxCharges()
    {
        return flyUpgradeTwoCharges ? flyMaxChargesUpgraded : flyMaxChargesBase;
    }

    private void HandleFlyChargeRecharge()
    {
        if (isFlying || flyCurrentCharges >= GetFlyMaxCharges())
            return;

        if (flyCooldownTimer > 0f)
        {
            flyCooldownTimer -= Time.deltaTime;
            return;
        }

        flyCurrentCharges++;
        if (flyCurrentCharges < GetFlyMaxCharges())
            flyCooldownTimer = flyCooldown;

        Debug.Log($"[Butterfly] Fly charge restored. Charges={flyCurrentCharges}/{GetFlyMaxCharges()}");
    }

    #endregion

    #region Ultimate - Tornado

    public void CastTornado(Vector3 position)
    {
        if (player == null) return;
        if (!player.isLocalPlayer()) return;

        if (tornadoCooldownTimer > 0f)
        {
            if (enableTornadoDebugLogs)
            {
                Debug.Log($"[Butterfly] Tornado blocked locally | remaining={Mathf.Max(0f, tornadoCooldownTimer):F2}s | playerId={player.GetPlayerID()}");
            }
            return;
        }

        if (enableTornadoDebugLogs)
        {
            Debug.Log($"[Butterfly] Tornado requested locally | playerId={player.GetPlayerID()} | localTimer={tornadoCooldownTimer:F2}s");
        }

        if (startTornadoCooldownLocallyOnInput)
            StartTornadoCooldownClient(GetFinalCooldown(tornadoCooldown));

        // Trigger animation locally
        if (animator != null)
            animator.SetTrigger("Tornado");

        Debug.Log($"[Butterfly] CastTornado on {gameObject.name} | Player ID: {player.GetPlayerID()} | Player is Local: {player.isLocalPlayer()}");
        Debug.Log("[Butterfly] Sending Tornado ServerRpc.");

        ServerSpawnTornadoRpc(position, player.transform.forward);
    }

    private void StartTornadoCooldownClient(float cooldown)
    {
        tornadoCooldownTimer = cooldown;
        UpdateAbilityBarCooldown(tornadoAbilityBarIndex, cooldown);

        if (enableTornadoDebugLogs)
        {
            Debug.Log($"[Butterfly] Tornado local cooldown started | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSpawnTornadoRpc(Vector3 position, Vector3 forwardDirection)
    {
        if (!isServer) return;
        float finalCooldown = GetFinalCooldown(tornadoCooldown);
        float serverTime = Time.time;
        float remaining = tornadoNextAllowedTimeServer - serverTime;

        if (enableTornadoDebugLogs)
        {
            Debug.Log($"[Butterfly] Tornado request on server | playerId={player.GetPlayerID()} | serverTime={serverTime:F2} | nextAllowed={tornadoNextAllowedTimeServer:F2} | remaining={Mathf.Max(0f, remaining):F2}s");
        }

        if (serverTime < tornadoNextAllowedTimeServer)
        {
            RejectTornadoCooldownClientRpc(Mathf.Max(0f, remaining));
            return;
        }

        tornadoNextAllowedTimeServer = serverTime + finalCooldown;

        // Play animation on all observers
        PlayAnimationObserversRpc("Tornado");

        Debug.Log($"[Butterfly] ServerSpawnTornadoRpc received on server. player id={player.GetPlayerID()}");
        ServerSpawnTornado(position, forwardDirection);
        SyncTornadoCooldownClientRpc(finalCooldown);
    }

    [ObserversRpc]
    private void SyncTornadoCooldownClientRpc(float cooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        tornadoCooldownTimer = Mathf.Max(tornadoCooldownTimer, cooldown);
        UpdateAbilityBarCooldown(tornadoAbilityBarIndex, cooldown);

        if (enableTornadoDebugLogs)
        {
            Debug.Log($"[Butterfly] Tornado cooldown synced | cooldown={cooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    [ObserversRpc]
    private void RejectTornadoCooldownClientRpc(float remainingCooldown)
    {
        if (player == null || !player.isLocalPlayer())
            return;

        tornadoCooldownTimer = Mathf.Max(tornadoCooldownTimer, remainingCooldown);

        if (remainingCooldown > 0f)
            UpdateAbilityBarCooldown(tornadoAbilityBarIndex, remainingCooldown);

        if (enableTornadoDebugLogs)
        {
            Debug.Log($"[Butterfly] Tornado rejected by server | remaining={remainingCooldown:F2}s | playerId={player.GetPlayerID()}");
        }
    }

    private void ServerSpawnTornado(Vector3 position, Vector3 forwardDirection)
    {
        Debug.Log($"[Butterfly] ServerSpawnTornado - prefab={tornadoPrefab}, networkManager={networkManager}");

        if (tornadoPrefab == null)
        {
            Debug.LogError("[Butterfly] tornadoPrefab is NULL!");
            return;
        }

        if (networkManager == null)
        {
            Debug.LogError("[Butterfly] networkManager is NULL!");
            return;
        }

        GameObject tornadoGO = Instantiate(tornadoPrefab, position, Quaternion.identity);

        TornadoArea tornado = tornadoGO.GetComponent<TornadoArea>();
        if (tornado == null)
        {
            Debug.LogError("[Butterfly] TornadoArea component missing from tornado prefab root!");
            Destroy(tornadoGO);
            return;
        }

        NetworkManager.main.Spawn(tornadoGO);

        tornado.SpawnSetup(
            player,
            tornadoRadius,
            tornadoDuration,
            tornadoBaseDamagePerTick,
            tornadoTickInterval,
            tornadoGroupForce,
            forwardDirection
        );

        Debug.Log($"[Butterfly] Instantiated tornado: {tornadoGO.name}. PurrNet will auto-sync via NetworkBehaviour.");
    }

    #endregion

    #region Animation Helpers

    [ObserversRpc]
    private void PlayAnimationObserversRpc(string triggerName)
    {
        // Don't replay on owner since they already triggered locally
        if (animator != null && !isOwner)
            animator.SetTrigger(triggerName);
    }

    #endregion
}