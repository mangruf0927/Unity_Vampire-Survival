using UnityEngine;

public class KnifeWeapon : Weapon
{
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField, Min(0.01f)] private float cooldown = 1f;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.1f;
    [SerializeField, Min(1)] private int penetrationCount = 1;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.01f)] private float speedMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float areaMultiplier = 1f;

    private float burstRemaining;
    private int shotsRemaining;

    public override string DisplayName => "Knife";
    protected override float BaseCooldown => cooldown;
    public float BurstInterval => Mathf.Max(0.01f, burstInterval);
    public int PenetrationCount => penetrationCount;

    private void Reset()
    {
        damage = 6.5f;
        knockback = 0.5f;
    }

    private void Awake()
    {
        if (projectilePrefab == null) enabled = false;
    }

    public override string GetChoiceDescription()
    {
        if (!IsOwned) return "마지막 이동 방향으로 단검을 일직선으로 발사합니다.";
        return (Level + 1) switch
        {
            2 => "투사체를 1개 더 발사합니다.",
            3 or 7 => "투사체 수가 1개, 피해량이 5 증가합니다.",
            4 or 6 => "투사체 수가 1개 증가하고 연사 간격이 0.02초 감소합니다.",
            5 => "관통 수가 1 증가합니다.",
            8 => "관통 수가 1 증가하고 연사 간격이 0.02초 감소합니다.",
            _ => "최대 레벨",
        };
    }

    protected override void UpgradeWeapon(int level)
    {
        if (level == 2 || level == 3 || level == 4 || level == 6 || level == 7) projectileCount++;
        if (level == 3 || level == 7) damage += 5f;
        if (level == 4 || level == 6 || level == 8) burstInterval -= 0.02f;
        if (level == 5 || level == 8) penetrationCount++;
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
        Projectile projectile = Instantiate(projectilePrefab, AttackOrigin.position,
            Quaternion.LookRotation(direction, Vector3.up));
        projectile.transform.localScale *= areaMultiplier;
        projectile.Initialize(direction, projectileSpeed * speedMultiplier, TargetCamera,
            Damage, PenetrationCount, Knockback, areaMultiplier);
    }
}
