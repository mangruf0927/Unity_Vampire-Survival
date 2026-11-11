using UnityEngine;

public class WhipWeapon : Weapon
{
    [SerializeField] private WhipSlash slashPrefab;
    private float cooldown = 1.35f;
    private float burstInterval = 0.1f;
    private float slashLength = 3f;
    private float slashWidth = 0.7f;
    private float slashDuration = 0.15f;

    private float areaMultiplier = 1f;
    private float burstRemaining;
    private int shotsRemaining;
    private float lastSide = 1f;
    private float nextSide;
    private Vector3 burstRight;

    protected override float BaseCooldown => cooldown;
    public float AreaMultiplier => areaMultiplier;

    private void Awake()
    {
        if (slashPrefab == null) enabled = false;
    }

    protected override void ApplyWeaponData(WeaponData data)
    {
        cooldown = data.cooldown;
        burstInterval = data.burstInterval;
        slashLength = data.slashLength;
        slashWidth = data.slashWidth;
        slashDuration = data.slashDuration;
        areaMultiplier = data.areaMultiplier;
    }

    protected override void UpgradeWeapon(int level)
    {
        base.UpgradeWeapon(level);
        WeaponUpgradeData upgrade = Data.GetUpgrade(level);
        cooldown += upgrade.cooldown;
        burstInterval += upgrade.burstInterval;
        areaMultiplier += upgrade.areaMultiplier;
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
            SoundManager.Instance?.PlaySFX(SoundManager.WhipAttack);
            nextSide = -nextSide;
            shotsRemaining--;
            burstRemaining += Mathf.Max(0.01f, burstInterval);
        }
    }
}
