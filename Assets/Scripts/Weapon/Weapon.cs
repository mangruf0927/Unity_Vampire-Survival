using UnityEngine;

public abstract class Weapon : MonoBehaviour, ILevelUpOption
{
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int dataId = 1;
    [SerializeField, Range(1, 8)] private int level = 1;

    private bool isOwned;
    private bool upgradesInitialized;

    protected float damage;
    protected int projectileCount;
    protected float knockback;
    protected float cooldownRemaining;

    private float damageMultiplier = 1f;
    private float cooldownMultiplier = 1f;
    private float characterDamageMultiplier = 1f;
    private int characterProjectileBonus;

    protected Transform AttackOrigin { get; private set; }
    protected Camera TargetCamera { get; private set; }
    protected Vector3 AttackDirection { get; private set; } = Vector3.forward;
    protected float CharacterProjectileSpeedMultiplier { get; private set; } = 1f;

    protected WeaponData Data => DataManager.Instance.Weapons.Get(dataId);
    protected abstract float BaseCooldown { get; }

    public int DataId => dataId;
    public bool IsOwned => isOwned;
    public Sprite Icon => icon;
    public float AcquisitionTime { get; internal set; }
    public int Level => isOwned ? Mathf.Clamp(level, 1, 8) : 0;
    public bool CanUpgrade => !isOwned || Level < 8;
    public string DisplayName => Data.displayName;

    public virtual float Damage => damage * damageMultiplier * characterDamageMultiplier;
    public virtual int ProjectileCount => projectileCount + characterProjectileBonus;
    public float Knockback => knockback;
    public float Cooldown => Mathf.Max(0.01f, BaseCooldown * cooldownMultiplier);

    protected virtual void OnEnable()
    {
        InitializeUpgrades();
    }

    protected virtual void OnDisable() { }

    protected virtual void LateUpdate()
    {
        if (!isOwned || AttackOrigin == null || Time.deltaTime <= 0f) return;

        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        UpdateAttack(Time.deltaTime);
    }

    public void Initialize(Transform attackOrigin, Camera targetCamera, Vector3 attackDirection)
    {
        AttackOrigin = attackOrigin;
        TargetCamera = targetCamera;
        SetAttackDirection(attackDirection);
    }

    public void SetAttackDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.000001f)
            AttackDirection = direction.normalized;
    }

    public void SetCharacterBonuses(float damageBonus, float speedBonus, int projectileBonus)
    {
        characterDamageMultiplier = damageBonus;
        CharacterProjectileSpeedMultiplier = speedBonus;
        characterProjectileBonus = projectileBonus;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetCooldownMultiplier(float multiplier)
    {
        cooldownMultiplier = Mathf.Max(0f, multiplier);
    }

    public string GetChoiceDescription()
    {
        if (!isOwned) return Data.acquisitionDescription;

        WeaponUpgradeData upgrade = Data.GetUpgrade(Level + 1);
        return upgrade != null ? upgrade.GetDescription() : "최대 레벨";
    }

    public void Acquire()
    {
        if (isOwned) return;

        WeaponData data = Data;

        damage = data.damage;
        projectileCount = data.projectileCount;
        knockback = data.knockback;

        ApplyWeaponData(data);

        isOwned = true;
        level = 1;
        InitializeUpgrades();
    }

    [ContextMenu("Level Up (Play Mode)")]
    public void LevelUp()
    {
        if (!Application.isPlaying || !isOwned || Level >= 8) return;

        InitializeUpgrades();
        level++;
        UpgradeWeapon(level);
    }

    private void InitializeUpgrades()
    {
        if (!isOwned || upgradesInitialized) return;

        upgradesInitialized = true;

        for (int i = 2; i <= Level; i++)
            UpgradeWeapon(i);
    }

    protected virtual void UpgradeWeapon(int level)
    {
        WeaponUpgradeData upgrade = Data.GetUpgrade(level);

        damage += upgrade.damage;
        projectileCount += upgrade.projectileCount;
    }

    protected void StartCooldown()
    {
        cooldownRemaining = Cooldown;
    }

    protected abstract void ApplyWeaponData(WeaponData data);
    protected abstract void UpdateAttack(float deltaTime);
}
