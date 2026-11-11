using System.Collections.Generic;
using UnityEngine;

public class KingBibleWeapon : Weapon
{
    [SerializeField] private BibleProjectile projectilePrefab;
    private float cooldown = 3f;
    private float duration = 3f;
    private float orbitRadius = 2f;
    private float rotationSpeed = 180f;
    private float hitboxDelay = 1.7f;

    private struct HitRecord
    {
        public int activationVersion;
        public float nextHitTime;
    }

    private readonly List<BibleProjectile> projectiles = new();
    private readonly Dictionary<EnemyMovement, HitRecord> hitRecords = new();
    private readonly List<EnemyMovement> expiredRecords = new();
    private bool attacking;
    private float remainingDuration;
    private float angle;
    private Vector3 previousCenter;
    private int activeCount;
    private float activeDamage;
    private float activeKnockback;
    private float activeRadius;
    private float activeSpeed;
    private float activeHitboxDelay;
    private float speedMultiplier = 1f;
    private float radiusMultiplier = 1f;

    protected override float BaseCooldown => cooldown;
    public float Duration => duration;
    public float OrbitRadius => orbitRadius * radiusMultiplier;
    public float RotationSpeed => rotationSpeed * speedMultiplier * CharacterProjectileSpeedMultiplier;

    protected override void ApplyWeaponData(WeaponData data)
    {
        cooldown = data.cooldown;
        duration = data.duration;
        orbitRadius = data.orbitRadius;
        rotationSpeed = data.rotationSpeed;
        hitboxDelay = data.hitboxDelay;
        speedMultiplier = data.speedMultiplier;
        radiusMultiplier = data.areaMultiplier;
    }

    protected override void UpgradeWeapon(int level)
    {
        base.UpgradeWeapon(level);
        WeaponUpgradeData upgrade = Data.GetUpgrade(level);
        cooldown += upgrade.cooldown;
        duration += upgrade.duration;
        speedMultiplier += upgrade.speedMultiplier;
        radiusMultiplier += upgrade.areaMultiplier;
    }

    private void Awake()
    {
        if (projectilePrefab == null) enabled = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAttack();
    }

    protected override void UpdateAttack(float deltaTime)
    {
        if (!attacking)
        {
            if (cooldownRemaining > 0f) return;
            BeginAttack();
            return;
        }

        float elapsed = Mathf.Min(deltaTime, remainingDuration);
        Vector3 center = AttackOrigin.position;

        float travel = Vector3.Distance(previousCenter, center) + activeRadius * activeSpeed * Mathf.Deg2Rad * elapsed;
        int steps = Mathf.Max(1, Mathf.CeilToInt(travel / 0.1f));
        float startAngle = angle;
        for (int step = 1; step <= steps; step++)
        {
            float t = (float)step / steps;
            Vector3 stepCenter = Vector3.Lerp(previousCenter, center, t);
            float stepAngle = startAngle + activeSpeed * elapsed * t;
            for (int i = 0; i < activeCount; i++)
            {
                Vector3 position = GetOrbitPosition(stepCenter, stepAngle, i);
                projectiles[i].MoveAndHit(position, this);
            }
        }
        angle = (startAngle + activeSpeed * elapsed) % 360f;
        previousCenter = center;
        remainingDuration -= elapsed;
        if (remainingDuration <= 0f)
        {
            StopAttack();
            StartCooldown();
        }
    }

    private Vector3 GetOrbitPosition(Vector3 center, float degrees, int index)
    {
        float radians = (degrees + 360f * index / activeCount) * Mathf.Deg2Rad;
        return center + new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * activeRadius;
    }

    private void BeginAttack()
    {
        SoundManager.Instance?.PlaySFX(SoundManager.KingBibleAttack);
        RemoveExpiredRecords();
        activeCount = ProjectileCount;
        activeDamage = Damage;
        activeKnockback = Knockback;
        activeRadius = OrbitRadius;
        activeSpeed = RotationSpeed;
        activeHitboxDelay = Mathf.Max(0.01f, hitboxDelay);
        remainingDuration = Duration;
        previousCenter = AttackOrigin.position;
        angle = 0f;
        attacking = true;

        while (projectiles.Count < activeCount)
            projectiles.Add(Instantiate(projectilePrefab, transform));
        for (int i = 0; i < activeCount; i++)
        {
            Vector3 position = GetOrbitPosition(previousCenter, angle, i);
            projectiles[i].transform.position = position;
            projectiles[i].gameObject.SetActive(true);
            projectiles[i].MoveAndHit(position, this);
        }
    }

    private void StopAttack()
    {
        attacking = false;
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (projectiles[i] != null) projectiles[i].gameObject.SetActive(false);
        }
    }

    internal void TryHit(EnemyMovement enemy)
    {
        if (!enemy.isActiveAndEnabled) return;
        if (hitRecords.TryGetValue(enemy, out HitRecord record) && record.activationVersion == enemy.ActivationVersion && Time.time < record.nextHitTime) return;
        hitRecords[enemy] = new HitRecord
        {
            activationVersion = enemy.ActivationVersion,
            nextHitTime = Time.time + activeHitboxDelay
        };
        Vector3 away = enemy.transform.position - AttackOrigin.position;
        enemy.ApplyKnockback(away, activeKnockback);
        if (enemy.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(activeDamage);
    }

    private void RemoveExpiredRecords()
    {
        expiredRecords.Clear();
        foreach (KeyValuePair<EnemyMovement, HitRecord> pair in hitRecords)
        {
            if (pair.Key == null || !pair.Key.isActiveAndEnabled || pair.Value.nextHitTime <= Time.time)
                expiredRecords.Add(pair.Key);
        }
        for (int i = 0; i < expiredRecords.Count; i++) hitRecords.Remove(expiredRecords[i]);
    }
}
