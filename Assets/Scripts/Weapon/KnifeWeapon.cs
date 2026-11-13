using UnityEngine;

public class KnifeWeapon : Weapon
{
    [SerializeField] private Projectile projectilePrefab;

    private float cooldown;
    private float burstInterval;
    private int penetrationCount;
    private float projectileSpeed;
    private float speedMultiplier;
    private float areaMultiplier;

    private float burstRemaining;
    private int shotsRemaining;

    protected override float BaseCooldown => cooldown;
    public float BurstInterval => Mathf.Max(0.01f, burstInterval);
    public int PenetrationCount => penetrationCount;

    private void Awake()
    {
        if (projectilePrefab == null) enabled = false;
    }

    protected override void ApplyWeaponData(WeaponData data)
    {
        cooldown = data.cooldown;
        burstInterval = data.burstInterval.Value;
        penetrationCount = data.penetrationCount.Value;
        projectileSpeed = data.projectileSpeed.Value;
        speedMultiplier = data.speedMultiplier.GetValueOrDefault(1f);
        areaMultiplier = data.areaMultiplier.GetValueOrDefault(1f);
    }

    protected override void UpgradeWeapon(int level)
    {
        base.UpgradeWeapon(level);
        WeaponUpgradeData upgrade = Data.GetUpgrade(level);
        cooldown += upgrade.cooldown;
        burstInterval += upgrade.burstInterval;
        speedMultiplier += upgrade.speedMultiplier;
        areaMultiplier += upgrade.areaMultiplier;
        penetrationCount += upgrade.penetrationCount;
    }

    protected override void UpdateAttack(float deltaTime)
    {
        if (TargetCamera == null) return;
        burstRemaining -= deltaTime;
        if (shotsRemaining == 0 && cooldownRemaining <= 0f)
        {
            StartCooldown();
            shotsRemaining = ProjectileCount;
            burstRemaining = 0f;
        }
        while (shotsRemaining > 0 && burstRemaining <= 0f)
        {
            Fire();
            shotsRemaining--;
            burstRemaining += BurstInterval;
        }
    }

    private void Fire()
    {
        Vector3 direction = AttackDirection;
        Projectile projectile = Instantiate(projectilePrefab, AttackOrigin.position, Quaternion.LookRotation(direction, Vector3.up));
        projectile.transform.localScale *= areaMultiplier;
        projectile.Initialize(direction, projectileSpeed * speedMultiplier * CharacterProjectileSpeedMultiplier, TargetCamera, Damage, PenetrationCount, Knockback, areaMultiplier);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.KnifeAttack);
    }
}
