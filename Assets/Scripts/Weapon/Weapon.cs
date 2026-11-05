using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    private bool isOwned;
    [SerializeField] private Sprite icon;
    [SerializeField, Range(1, 8)] private int level = 1;
    [SerializeField, Min(0)] protected int damage = 10;
    [SerializeField, Min(1)] protected int projectileCount = 1;
    [SerializeField, Min(0f)] protected float knockback = 1f;

    private bool upgradesInitialized;
    protected float cooldownRemaining;

    public bool IsOwned => isOwned;
    public Sprite Icon => icon;
    public int Level => isOwned ? Mathf.Clamp(level, 1, 8) : 0;
    public bool CanUpgrade => !isOwned || Level < 8;
    public abstract string DisplayName { get; }
    public abstract string GetChoiceDescription();
    public virtual int Damage => damage;
    public virtual int ProjectileCount => projectileCount;
    public float Knockback => knockback;
    public abstract float Cooldown { get; }

    protected virtual void OnEnable()
    {
        InitializeUpgrades();
    }

    protected virtual void OnDisable() { }

    public void Acquire()
    {
        if (isOwned) return;
        isOwned = true;
        level = 1;
        InitializeUpgrades();
    }

    [ContextMenu("Level Up (Play Mode)")]
    public void LevelUp()
    {
        if (!Application.isPlaying || !isOwned || Level >= 8) return;
        InitializeUpgrades();
        level = Level + 1;
        UpgradeWeapon(level);
    }

    private void InitializeUpgrades()
    {
        if (!isOwned || upgradesInitialized) return;
        upgradesInitialized = true;
        // Inspector에서 지정한 시작 레벨의 강화도 순서대로 한 번만 적용한다.
        for (int i = 2; i <= Level; i++) UpgradeWeapon(i);
    }

    protected virtual void UpgradeWeapon(int level) { }

    protected virtual void LateUpdate()
    {
        if (!isOwned || Time.deltaTime <= 0f) return;
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        UpdateAttack(Time.deltaTime);
    }

    protected void StartCooldown()
    {
        cooldownRemaining = Cooldown;
    }

    protected abstract void UpdateAttack(float deltaTime);
}
