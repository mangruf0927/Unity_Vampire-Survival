using UnityEngine;

// 전투/사망을 검증하되 보석·사운드·게임 오버는 발생시키지 않는 테스트 체력.
public class ComparisonHealth : MonoBehaviour, IDamageable
{
    public EnemyCrowdComparison Owner { get; private set; }
    public int Index { get; set; } = -1;
    public bool IsSwarm { get; set; }
    public bool IsDead => health <= 0f;
    public int Hits => hits;
    public float DamageTaken { get; private set; }
    [SerializeField] private float health;
    [SerializeField] private int hits;
    private float damageInterval;
    private float nextDamageTime;
    private bool isPlayer;

    public void Initialize(EnemyCrowdComparison owner, int index, float maxHealth, float interval = 0f)
    {
        Owner = owner;
        Index = index;
        IsSwarm = false;
        isPlayer = owner == null;
        health = maxHealth;
        damageInterval = interval;
        nextDamageTime = 0f;
        hits = 0;
        DamageTaken = 0f;
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f || health <= 0f || Time.timeScale <= 0f || Time.time < nextDamageTime) return;
        health = Mathf.Max(0f, health - damage);
        hits++;
        DamageTaken += damage;
        nextDamageTime = Time.time + damageInterval;
        if (!isPlayer && health <= 0f) ObjectPool.Instance.TryReturnToPool(gameObject);
    }

    private void OnDisable()
    {
        if (Owner != null) Owner.RemoveEnemy(this);
        Owner = null;
        Index = -1;
    }
}
