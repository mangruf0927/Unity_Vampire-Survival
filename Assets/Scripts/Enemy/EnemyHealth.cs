using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float maxHealth = 5f;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth > 0f) return;

        isDead = true;

        if (!ObjectPool.Instance.TryReturnToPool(gameObject))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
