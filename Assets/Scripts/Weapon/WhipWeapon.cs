using UnityEngine;

public class WhipWeapon : Weapon
{
    [SerializeField] private WhipSlash slashPrefab;
    [SerializeField, Min(0.01f)] private float cooldown = 1.35f;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.1f;
    [SerializeField, Min(0.01f)] private float slashLength = 3f;
    [SerializeField, Min(0.01f)] private float slashWidth = 0.7f;
    [SerializeField, Min(0.01f)] private float slashDuration = 0.15f;

    private float areaMultiplier = 1f;
    private float burstRemaining;
    private int shotsRemaining;
    private float lastSide = 1f;
    private float nextSide;
    private Vector3 burstRight;

    public override string DisplayName => "Whip";
    protected override float BaseCooldown => cooldown;
    public float AreaMultiplier => areaMultiplier;

    private void Awake()
    {
        if (slashPrefab == null) enabled = false;
    }

    public override string GetChoiceDescription()
    {
        if (!IsOwned) return "화면 좌우로 길게 베어 범위 안의 적들을 관통 공격합니다.";
        int nextLevel = Level + 1;
        if (nextLevel == 2) return "공격을 1회 더 가합니다.";
        if (nextLevel == 4 || nextLevel == 6) return "피해량이 5, 공격 범위가 10% 증가합니다.";
        return Level < 8 ? "피해량이 5 증가합니다." : "최대 레벨";
    }

    protected override void UpgradeWeapon(int level)
    {
        if (level == 2) projectileCount++;
        if (level >= 3 && level <= 8) damage += 5f;
        if (level == 4 || level == 6) areaMultiplier += 0.1f;
    }

    protected override void UpdateAttack(float deltaTime)
    {
        if (TargetCamera == null) return;
        Vector3 right = TargetCamera.transform.right;
        right.y = 0f;
        if (right.sqrMagnitude <= 0.000001f) return;
        right.Normalize();
        float horizontal = Vector3.Dot(AttackDirection, right);
        if (Mathf.Abs(horizontal) > 0.001f) lastSide = horizontal > 0f ? 1f : -1f;

        burstRemaining -= deltaTime;
        if (shotsRemaining == 0 && cooldownRemaining <= 0f)
        {
            StartCooldown();
            shotsRemaining = ProjectileCount;
            burstRemaining = 0f;
            burstRight = right;
            nextSide = lastSide;
        }
        while (shotsRemaining > 0 && burstRemaining <= 0f)
        {
            Vector3 direction = burstRight * nextSide;
            WhipSlash slash = Instantiate(slashPrefab, transform);
            slash.Initialize(AttackOrigin, direction, Damage, Knockback,
                slashLength * areaMultiplier, slashWidth * areaMultiplier, slashDuration);
            nextSide = -nextSide;
            shotsRemaining--;
            burstRemaining += Mathf.Max(0.01f, burstInterval);
        }
    }
}
