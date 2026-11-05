using UnityEngine;

public class MagicWand : Weapon
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MagicProjectile projectilePrefab;
    [SerializeField, Min(0.01f)] private float cooldown = 1.2f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;

    [SerializeField, Min(0.01f)] private float burstInterval = 0.1f;
    [SerializeField, Min(1)] private int penetrationCount = 1;

    public override float Cooldown => Mathf.Max(0.01f, cooldown);
    public int PenetrationCount => penetrationCount;

    protected override void UpgradeWeapon(int level)
    {
        if (level == 2 || level == 4 || level == 6) projectileCount++;
        if (level == 3) cooldown -= 0.2f;
        if (level == 5 || level == 8) damage += 10;
        if (level == 7) penetrationCount++;
    }

    private float burstRemaining;
    private int shotsRemaining;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || projectilePrefab == null) enabled = false;
    }

    protected override void UpdateAttack(float deltaTime)
    {
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

        Vector3 direction = target.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        MagicProjectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(direction, Vector3.up));
        projectile.Initialize(direction.normalized, projectileSpeed, targetCamera, Damage, PenetrationCount, Knockback);
    }


    private EnemyMovement FindNearestVisibleEnemy()
    {
        EnemyMovement nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Vector3 origin = transform.position;
        for (int i = 0; i < EnemyCrowd.ActiveCount; i++)
        {
            EnemyMovement enemy = EnemyCrowd.GetActiveEnemy(i);
            if (enemy == null || !enemy.isActiveAndEnabled) continue;

            Vector3 position = enemy.transform.position;
            float distance = (position - origin).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            if (!MagicProjectile.IsInsideView(targetCamera, position)) continue;
            nearestDistance = distance;
            nearest = enemy;
        }
        return nearest;
    }
}
