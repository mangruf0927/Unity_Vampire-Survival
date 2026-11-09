using UnityEngine;

public class MagicWandWeapon : Weapon
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField, Min(0.01f)] private float cooldown = 1.2f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;

    [SerializeField, Min(0.01f)] private float burstInterval = 0.1f;
    [SerializeField, Min(1)] private int penetrationCount = 1;

    public override string DisplayName => "Magic Wand";
    public override string GetChoiceDescription()
    {
        if (!IsOwned) return "화면 안의 가장 가까운 적을 향해 마법 투사체를 발사합니다.";
        int nextLevel = Level + 1;
        if (nextLevel == 2 || nextLevel == 4 || nextLevel == 6) return "투사체를 1개 더 발사합니다.";
        if (nextLevel == 3) return "쿨타임이 0.2초 감소합니다.";
        if (nextLevel == 5 || nextLevel == 8) return "피해량이 10 증가합니다.";
        if (nextLevel == 7) return "관통 수가 1 증가합니다.";
        return "최대 레벨";
    }

    protected override float BaseCooldown => cooldown;
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
        projectile.Initialize(direction.normalized, projectileSpeed, TargetCamera, Damage, PenetrationCount, Knockback);
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
