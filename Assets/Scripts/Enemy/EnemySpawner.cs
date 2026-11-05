using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private PoolTypeEnums enemyPoolType = PoolTypeEnums.Bat;
    [SerializeField, Min(0.01f)] private float spawnInterval = 1f;
    [SerializeField, Min(0)] private int minimumEnemies = 15;
    [SerializeField, Min(0)] private int maximumEnemies = 50;
    [SerializeField] private float spawnHeight = 1f;
    [SerializeField, Min(0.1f)] private float spawnMargin = 2f;

    private readonly Vector3[] corners = new Vector3[4];
    private const float OffscreenCheckInterval = 0.2f;
    private float nextOffscreenCheckTime;
    private float remainingTime;
    private float enemyRadius;
    private bool isReady;

    private void Start()
    {
        if (gameCamera == null) gameCamera = Camera.main;

        if (player == null || gameCamera == null
            || !ObjectPool.Instance.TryGetPrefab(enemyPoolType, out GameObject prefab)
            || !prefab.TryGetComponent(out EnemyMovement movement)
            || !prefab.TryGetComponent<EnemyHealth>(out _))
        {
            Debug.LogWarning("EnemySpawner의 Player, Camera와 PoolDataInitializer의 적 풀 등록을 확인해 주세요.", this);
            enabled = false;
            return;
        }

        enemyRadius = movement.BodyRadius;
        isReady = true;
    }

    private void LateUpdate()
    {
        if (!isReady || player == null || gameCamera == null || Time.deltaTime <= 0f) return;

        if (Time.time >= nextOffscreenCheckTime)
        {
            ReturnOffscreenEnemies();
            nextOffscreenCheckTime = Time.time + OffscreenCheckInterval;
        }

        remainingTime -= Time.deltaTime;
        if (remainingTime > 0f) return;

        remainingTime = Mathf.Max(0.01f, spawnInterval);
        SpawnEnemies();
    }

    private void ReturnOffscreenEnemies()
    {
        float now = Time.time;
        for (int i = EnemyCrowd.ActiveCount - 1; i >= 0; i--)
        {
            EnemyMovement enemy = EnemyCrowd.GetActiveEnemy(i);
            Vector3 viewport = gameCamera.WorldToViewportPoint(enemy.transform.position);
            bool inside = viewport.z >= gameCamera.nearClipPlane && viewport.z <= gameCamera.farClipPlane
                && viewport.x >= 0f && viewport.x <= 1f
                && viewport.y >= 0f && viewport.y <= 1f;
            if (!enemy.ShouldReturnOffscreen(inside, now)) continue;

            ObjectPool.Instance.TryReturnToPool(enemy.gameObject);
        }
    }

    private void SpawnEnemies()
    {
        int maximum = Mathf.Max(0, maximumEnemies);
        int minimum = Mathf.Clamp(minimumEnemies, 0, maximum);
        int alive = EnemyCrowd.ActiveCount;
        int count = Mathf.Min(minimum - alive, maximum - alive);

        if (count <= 0 || !UpdateScreenBoundary()) return;

        for (int i = 0; i < count; i++)
        {
            if (EnemyCrowd.ActiveCount >= maximum) break;

            Vector3 position = GetSpawnPosition();
            GameObject enemy = ObjectPool.Instance.GetFromPool(enemyPoolType, position, Quaternion.identity);
            if (enemy == null) break;

            enemy.GetComponent<EnemyMovement>().SetTarget(player);
        }
    }

    private bool UpdateScreenBoundary()
    {
        Plane plane = new(Vector3.up, new Vector3(0f, spawnHeight, 0f));
        for (int i = 0; i < 4; i++)
        {
            float x = i == 1 || i == 2 ? 1f : 0f;
            float y = i >= 2 ? 1f : 0f;
            Ray ray = gameCamera.ViewportPointToRay(new Vector3(x, y, 0f));
            if (!plane.Raycast(ray, out float distance)) return false;
            corners[i] = ray.GetPoint(distance);
        }
        return true;
    }

    private Vector3 GetSpawnPosition()
    {
        int edge = Random.Range(0, 4);
        Vector3 a = corners[edge];
        Vector3 b = corners[(edge + 1) % 4];
        Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
        Vector3 outward = Vector3.Cross(b - a, Vector3.up).normalized;
        if (Vector3.Dot(outward, (a + b) * 0.5f - center) < 0f) outward = -outward;
        Vector3 position = Vector3.Lerp(a, b, Random.value) + outward * (spawnMargin + enemyRadius);
        position.y = spawnHeight;
        return position;
    }
}
