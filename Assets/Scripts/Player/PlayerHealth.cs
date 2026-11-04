using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float maxHealth = 100f;
    [SerializeField, Min(0.01f)] private float damageInterval = 1f;

    private float currentHealth;
    private float nextDamageTime;
    private bool isDead;
    private float previousTimeScale = 1f;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0f || Time.timeScale <= 0f || Time.time < nextDamageTime) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        nextDamageTime = Time.time + Mathf.Max(0.01f, damageInterval);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        isDead = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Debug.Log("Game Over: 플레이어가 사망했습니다.", this);
    }

    private void OnDestroy()
    {
        if (isDead && Time.timeScale == 0f) Time.timeScale = previousTimeScale;
    }
}
