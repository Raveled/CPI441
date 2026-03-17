using System.Collections;
using UnityEngine;

public class Butterfly : MonoBehaviour
{
    [Header("Entity Reference")]
    public Entity entity;

    [Header("Basic Attack - Wind Burst")]
    [SerializeField] private GameObject windBurstProjectilePrefab;
    [SerializeField] private Transform windBurstFirePoint;
    [SerializeField] private int windBurstBaseDamage = 4;
    [SerializeField] private float windBurstSpeed = 10f;
    [SerializeField] private float windBurstRange = 6f;

    [Header("Dust Wave - Ability 1")]
    [SerializeField] private GameObject dustWavePrefab;
    [SerializeField] private Transform dustWaveOrigin;
    [SerializeField] private int dustWaveBaseDamage = 3;
    [SerializeField] private float dustWaveRadius = 4f;
    [SerializeField] private float dustWaveCooldown = 5f;

    [Header("Dazzling Wave - Ability 2")]
    [SerializeField] private GameObject dazzlingWavePrefab;
    [SerializeField] private Transform dazzlingWaveOrigin;
    [SerializeField] private int dazzlingWaveBaseDamage = 2;
    [SerializeField] private float dazzlingWaveRadius = 4f;
    [SerializeField] private int dazzlingWaveHealAmount = 10;
    [SerializeField] private float dazzlingWaveCooldown = 6f;
    [SerializeField] private bool dazzlingWaveUpgradeReducedDamage = false;
    [SerializeField] private float dazzlingWaveDamageReductionMultiplier = 0.7f;

    [Header("Fly - Ability 3")]
    [SerializeField] private float flyDashDistance = 8f;
    [SerializeField] private float flyDashDuration = 0.35f;
    [SerializeField] private float flyCooldown = 7f;
    [SerializeField] private bool flyUpgradeTwoCharges = false;
    [SerializeField] private int flyMaxChargesBase = 1;
    [SerializeField] private int flyMaxChargesUpgraded = 2;

    [Header("Tornado - Ultimate")]
    [SerializeField] private GameObject tornadoPrefab;
    [SerializeField] private float tornadoRadius = 5f;
    [SerializeField] private float tornadoDuration = 4f;
    [SerializeField] private int tornadoBaseDamagePerTick = 2;
    [SerializeField] private float tornadoTickInterval = 0.5f;
    [SerializeField] private float tornadoGroupForce = 10f;

    // Runtime state
    private float dustWaveCooldownTimer = 0f;
    private float dazzlingWaveCooldownTimer = 0f;
    private float flyCooldownTimer = 0f;
    private int flyCurrentCharges = 0;
    private bool isFlying = false;
    private float flyTimer = 0f;
    private Vector3 flyDirection;
    private Vector3 flyDashHeight = new Vector3(0, 5, 0);
    private Vector3 flyStartPos;
    private Rigidbody flyRB;

    private void Awake()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        flyRB = GetComponent<Rigidbody>();
        flyCurrentCharges = GetFlyMaxCharges();
    }

    private void Update()
    {
        if (dustWaveCooldownTimer > 0f)
            dustWaveCooldownTimer -= Time.deltaTime;

        if (dazzlingWaveCooldownTimer > 0f)
            dazzlingWaveCooldownTimer -= Time.deltaTime;

        if (flyCooldownTimer > 0f)
            flyCooldownTimer -= Time.deltaTime;

        if (isFlying)
            HandleFlyMovement();

        HandleFlyChargeRecharge();
    }

    #region Basic Attack - Wind Burst

    public void CastWindBurst()
    {
        Debug.Log("Casting Wind Burst");

        if (windBurstProjectilePrefab == null || windBurstFirePoint == null)
            return;

        Entity shooter = entity ?? GetComponent<Entity>();

        GameObject projGO = Instantiate(windBurstProjectilePrefab, windBurstFirePoint.position, windBurstFirePoint.rotation);
        WindBurstProjectile proj = projGO.GetComponent<WindBurstProjectile>();
        if (proj != null)
        {
            proj.ownerEntity = shooter;
            proj.damage = windBurstBaseDamage;
            proj.maxRange = windBurstRange;
        }
        StartCoroutine(DestroyAfterDelay(projGO, 1.5f));
    }

    #endregion

    #region Ability 1 - Dust Wave

    public void CastDustWave()
    {
        if (dustWaveCooldownTimer > 0f) return;

        Entity shooter = entity ?? GetComponent<Entity>();

        if (dustWavePrefab != null && dustWaveOrigin != null)
        {
            GameObject wave = Instantiate(dustWavePrefab, dustWaveOrigin.position, dustWaveOrigin.rotation);

            // Pass all fields to the projectile - it handles damage on its own tick
            DustWaveMovement proj = wave.GetComponent<DustWaveMovement>();
            if (proj != null)
            {
                proj.ownerEntity = shooter;
                proj.radius = dustWaveRadius;
                proj.damage = dustWaveBaseDamage;
            }

            StartCoroutine(DestroyAfterDelay(wave, 5f));
        }

        dustWaveCooldownTimer = dustWaveCooldown;
    }

    #endregion

    #region Ability 2 - Dazzling Wave

    public void CastDazzlingWave()
    {
        if (dazzlingWaveCooldownTimer > 0f) return;

        Entity shooter = entity ?? GetComponent<Entity>();

        if (dazzlingWavePrefab != null && dazzlingWaveOrigin != null)
        {
            GameObject wave = Instantiate(dazzlingWavePrefab, dazzlingWaveOrigin.position, dazzlingWaveOrigin.rotation);

            // Pass all fields to the projectile - it handles damage/heal on its own tick
            DazzlingWaveProjectile proj = wave.GetComponent<DazzlingWaveProjectile>();
            if (proj != null)
            {
                proj.ownerEntity = shooter;
                proj.radius = dazzlingWaveRadius;
                proj.damage = dazzlingWaveBaseDamage;
                proj.healAmount = dazzlingWaveHealAmount;
                proj.upgradeReducedDamage = dazzlingWaveUpgradeReducedDamage;
                proj.damageReductionMultiplier = dazzlingWaveDamageReductionMultiplier;
            }

            StartCoroutine(DestroyAfterDelay(wave, 5f));
        }

        dazzlingWaveCooldownTimer = dazzlingWaveCooldown;
    }

    #endregion

    #region Ability 3 - Fly

    public void StartFly(Vector3 direction)
    {
        if (isFlying || flyCurrentCharges <= 0)
            return;

        isFlying = true;
        flyTimer = flyDashDuration;
        flyDirection = direction.normalized;
        flyStartPos = transform.position;
        flyCurrentCharges--;

        if (flyRB != null)
            flyRB.useGravity = false;
    }

    public void CancelFly()
    {
        if (!isFlying) return;
        EndFly();
    }

    private void HandleFlyMovement()
    {
        flyTimer -= Time.deltaTime;
        transform.position = Vector3.Lerp(flyStartPos, flyStartPos + flyDirection * flyDashDistance + flyDashHeight, 1f - (flyTimer / flyDashDuration));

        if (flyTimer <= 0f)
            EndFly();
    }

    private void EndFly()
    {
        isFlying = false;
        flyTimer = 0f;
        if (flyRB != null)
            flyRB.useGravity = true;
        flyCooldownTimer = flyCooldown;
    }

    private int GetFlyMaxCharges()
    {
        return flyUpgradeTwoCharges ? flyMaxChargesUpgraded : flyMaxChargesBase;
    }

    private void HandleFlyChargeRecharge()
    {
        if (isFlying || flyCurrentCharges >= GetFlyMaxCharges())
            return;

        if (flyCooldownTimer <= 0f)
        {
            flyCurrentCharges++;
            flyCooldownTimer = flyCooldown;
        }
    }

    #endregion

    #region Ultimate - Tornado

    public void CastTornado(Vector3 position)
    {
        if (tornadoPrefab == null)
        {
            Debug.LogError("[Tornado] Prefab is not assigned in the Inspector!");
            return;
        }

        Entity shooter = entity ?? GetComponent<Entity>();
        GameObject tornadoGO = Instantiate(tornadoPrefab, position, Quaternion.identity);
        TornadoArea tornado = tornadoGO.GetComponent<TornadoArea>();

        if (tornado != null)
        {
            tornado.ownerEntity = shooter;
            tornado.radius = tornadoRadius;
            tornado.duration = tornadoDuration;
            tornado.damagePerTick = tornadoBaseDamagePerTick;
            tornado.tickInterval = tornadoTickInterval;
            tornado.groupForce = tornadoGroupForce;
            tornado.travelDirection = transform.forward;
            tornado.Init();
        }
        else
        {
            Debug.LogError("[Tornado] TornadoArea component missing from prefab root!");
        }
    }

    #endregion

    #region Helpers

    private IEnumerator DestroyAfterDelay(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
    }

    #endregion
}