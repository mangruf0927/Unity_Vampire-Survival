using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float maxHealth = 5f;
    [SerializeField, Range(0f, 1f)] private float gemDropChance = 0.6f;
    [SerializeField] private LayerMask groundLayers = 1 << 9;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth > 0f) return;

        isDead = true;
        DropExperienceGem();

        if (!ObjectPool.Instance.TryReturnToPool(gameObject))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private void DropExperienceGem()
    {
        if (gemDropChance <= 0f || (gemDropChance < 1f && Random.value >= gemDropChance)) return;
        if (!ObjectPool.Instance.TryGetPrefab(PoolTypeEnums.ExperienceGem, out GameObject prefab)) return;
        if (!prefab.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null) return;

        Vector3 origin = transform.position + Vector3.up;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers, QueryTriggerInteraction.Ignore)) return;

        float halfHeight = meshFilter.sharedMesh.bounds.extents.y * Mathf.Abs(prefab.transform.localScale.y);
        Vector3 position = hit.point + Vector3.up * halfHeight;
        ObjectPool.Instance.GetFromPool(PoolTypeEnums.ExperienceGem, position, Quaternion.identity);
    }

}
