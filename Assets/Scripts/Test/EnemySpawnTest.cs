using UnityEngine;
using UnityEngine.InputSystem;

public class EnemySpawnTest : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private PoolTypeEnums enemyType = PoolTypeEnums.Bat;
    [SerializeField, Min(0f)] private float minimumRadius = 5f;
    [SerializeField, Min(0.1f)] private float maximumRadius = 18f;
    [SerializeField] private float spawnHeight = 1f;

    private void Start()
    {
        if (player != null) return;
        Debug.LogError("EnemySpawnTest: Player를 연결해 주세요.", this);
        enabled = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (player == null || keyboard == null || Time.timeScale <= 0f) return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SpawnEnemies(100);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) SpawnEnemies(300);
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) SpawnEnemies(500);
    }

    private void SpawnEnemies(int count)
    {
        if (!ObjectPool.Instance.TryGetPrefab(enemyType, out GameObject prefab)
            || !prefab.TryGetComponent(out EnemyMovement prefabMovement)
            || !prefab.TryGetComponent(out EnemyHealth _))
        {
            Debug.LogWarning("EnemySpawnTest: EnemyMovement와 EnemyHealth가 있는 적 풀을 선택해 주세요.", this);
            return;
        }

        float inner = Mathf.Max(0f, minimumRadius);
        float outer = Mathf.Max(inner + 0.1f, maximumRadius);
        Vector3 center = player.position;
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            // 원환의 면적에 고르게 분포하도록 반지름의 제곱을 기준으로 추첨한다.
            float radius = Mathf.Sqrt(Random.Range(inner * inner, outer * outer));
            Vector3 position = new(center.x + Mathf.Cos(angle) * radius, spawnHeight,
                center.z + Mathf.Sin(angle) * radius);
            position = LegacyGridEnemyObstacleCollision.ResolveOverlap(position, prefabMovement.BodyRadius);
            Vector3 direction = center - position;
            direction.y = 0f;
            Quaternion rotation = direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity;

            GameObject enemy = ObjectPool.Instance.GetFromPool(enemyType, position, rotation);
            if (enemy == null) break;
            enemy.GetComponent<EnemyMovement>().SetTarget(player);
            spawned++;
        }
        Debug.Log($"EnemySpawnTest: {spawned}/{count}마리 추가 생성, 현재 활성 적 {EnemyCrowd.ActiveCount}마리", this);
    }
}
