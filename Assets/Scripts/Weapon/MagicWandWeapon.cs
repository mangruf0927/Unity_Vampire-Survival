using UnityEngine;

public class MagicWandWeapon : Weapon
{
    [SerializeField] private Projectile projectilePrefab;
    private float cooldown = 1.2f;
    private float projectileSpeed = 12f;

    private float burstInterval = 0.1f;
    private int penetrationCount = 1;

    protected override float BaseCooldown => cooldown;
    public int PenetrationCount => penetrationCount;

    protected override void ApplyWeaponData(WeaponData data)
    {
        cooldown = data.cooldown;
        projectileSpeed = data.projectileSpeed;
        burstInterval = data.burstInterval;
        penetrationCount = data.penetrationCount;
    }

    protected override void UpgradeWeapon(int level)
    {
        base.UpgradeWeapon(level);
        WeaponUpgradeData upgrade = Data.GetUpgrade(level);
        cooldown += upgrade.cooldown;
        burstInterval += upgrade.burstInterval;
        penetrationCount += upgrade.penetrationCount;
    }

    private float burstRemaining;
    private int shotsRemaining;

    private void Awake()
    {
        if (projectilePrefab == null) enabled = false;
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
            burstRemaining += Mathf.Max(0.01f, burstInterval);
        }
    }

    private void Fire()
    {
        EnemyMovement target = FindNearestVisibleEnemy();
        if (target == null) return;

        Vector3 direction = target.transform.position - AttackOrigin.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        Projectile projectile = Instantiate(projectilePrefab, AttackOrigin.position, Quaternion.LookRotation(direction, Vector3.up));
        projectile.Initialize(direction.normalized, projectileSpeed * CharacterProjectileSpeedMultiplier, TargetCamera, Damage, PenetrationCount, Knockback);
    }

    private EnemyMovement FindNearestVisibleEnemy()
    {
        EnemyMovement nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Vector3 origin = AttackOrigin.position;
        for (int i = 0; i < EnemyCrowd.ActiveCount; i++)
        {
            EnemyMovement enemy = EnemyCrowd.GetActiveEnemy(i);
            if (enemy == null || !enemy.isActiveAndEnabled) continue;

            Vector3 position = enemy.transform.position;
            float distance = (position - origin).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            if (!Projectile.IsInsideView(TargetCamera, position)) continue;
            nearestDistance = distance;
            nearest = enemy;
        }
        return nearest;
    }
}
