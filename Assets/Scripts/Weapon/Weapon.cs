using UnityEngine;

public abstract class Weapon : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField, Range(1, 8)] private int level = 1;
    [SerializeField, Min(0)] protected int damage = 10;
    [SerializeField, Min(1)] protected int projectileCount = 1;
    [SerializeField, Min(0f)] protected float knockback = 1f;

    private int lastPlayerLevel = 1;
    private bool upgradesInitialized;
    protected float cooldownRemaining;

    public int Level => Mathf.Clamp(level, 1, 8);
    public virtual int Damage => damage;
    public virtual int ProjectileCount => projectileCount;
    public float Knockback => knockback;
    public abstract float Cooldown { get; }

    protected virtual void OnEnable()
    {
        InitializeUpgrades();
        if (playerExperience == null) return;
        playerExperience.AddObserver(this);
        Notify();
    }

    protected virtual void OnDisable()
    {
        if (playerExperience != null) playerExperience.RemoveObserver(this);
    }

    public void Notify()
    {
        if (playerExperience == null) return;
        int gainedLevels = playerExperience.Level - lastPlayerLevel;
        lastPlayerLevel = playerExperience.Level;
        for (int i = 0; i < gainedLevels && Level < 8; i++) LevelUp();
    }

    [ContextMenu("Level Up (Play Mode)")]
    public void LevelUp()
    {
        if (!Application.isPlaying || Level >= 8) return;
        InitializeUpgrades();
        level = Level + 1;
        UpgradeWeapon(level);
    }

    private void InitializeUpgrades()
    {
        if (upgradesInitialized) return;
        upgradesInitialized = true;
        // Inspector에서 지정한 시작 레벨의 강화도 순서대로 한 번만 적용한다.
        for (int i = 2; i <= Level; i++) UpgradeWeapon(i);
    }

    protected virtual void UpgradeWeapon(int level) { }

    protected virtual void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        UpdateAttack(Time.deltaTime);
    }

    protected void StartCooldown()
    {
        cooldownRemaining = Cooldown;
    }

    protected abstract void UpdateAttack(float deltaTime);
}
