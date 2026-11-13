using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [FormerlySerializedAs("gameCamera")]
    [SerializeField] private Camera spawnCamera;
    [System.Serializable]
    private class EnemyEntry
    {
        public PoolTypeEnums poolType;
        [Min(0f)] public float weight = 1f;
    }

    [System.Serializable]
    private class SpawnStage
    {
        [Min(0f)] public float startTime;
        public EnemyEntry[] enemies;
        [Min(0.01f)] public float spawnInterval = 1f;
        [Min(0)] public int minimumEnemies = 15;
        [Min(0)] public int maximumEnemies = 50;
    }

    [SerializeField] private TimeSystem timeSystem;
    [SerializeField] private SpawnStage[] stages = new SpawnStage[10];
    [SerializeField, Min(0.01f)] private float offscreenCheckInterval = 0.2f;
    private float nextOffscreenCheckTime;

    [SerializeField, Min(0.1f)] private float spawnMargin = 2f;

    private readonly List<EventEnemyData> eventEnemies = new();
    private int[] nextEventIndices;
    private float[] eventBoundsRadii;

    private readonly List<Vector3> formationOffsets = new();
    private readonly List<Renderer> enemyRenderers = new();
    private CameraView cameraView;
    private Plane[] cameraPlanes;

    private float spawnTimer;
    private int currentStage = -1;
    [SerializeField] private float spawnHeight = 1f;

    private readonly Vector3[] corners = new Vector3[4];
    private bool cameraCornersReady;
    private Vector3 screenCenter;
    private Vector3 spawnEdgeNormal;

    private void Start()
    {
        if (player == null || spawnCamera == null || timeSystem == null ||
            stages == null || stages.Length != 10)
        {
            Debug.LogError("EnemySpawner: Player, Camera, Time System과 10개 구간을 설정해 주세요.", this);
            enabled = false;
            return;
        }

        if (!spawnCamera.TryGetComponent(out cameraView))
        {
            Debug.LogError("EnemySpawner: Camera에 CameraView를 추가해 주세요.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < stages.Length; i++)
        {
            SpawnStage stage = stages[i];
            if (stage == null || stage.enemies == null || stage.enemies.Length == 0 ||
                stage.spawnInterval <= 0f || stage.minimumEnemies < 0 || stage.maximumEnemies < 0)
            {
                Debug.LogError($"EnemySpawner: {i}번째 구간의 생성 설정을 확인해 주세요.", this);
                enabled = false;
                return;
            }
            for (int j = 0; j < stage.enemies.Length; j++)
            {
                EnemyEntry entry = stage.enemies[j];
                if (entry == null || entry.weight <= 0f) continue;
                if (!ValidatePool(entry.poolType)) return;
            }
        }
        float elapsed = timeSystem.ElapsedSeconds;
        foreach (EventEnemyData data in DataManager.Instance.EventEnemies.All.Values) eventEnemies.Add(data);
        nextEventIndices = new int[eventEnemies.Count];
        eventBoundsRadii = new float[eventEnemies.Count];
        for (int i = 0; i < eventEnemies.Count; i++)
        {
            EventEnemyData data = eventEnemies[i];
            if (!ValidatePool(data.poolType, true)) return;
            ObjectPool.Instance.TryGetPrefab(data.poolType, out GameObject prefab);
            float radius = prefab.GetComponent<EnemyMovement>().BodyRadius;
            prefab.GetComponentsInChildren<Renderer>(enemyRenderers);
            for (int j = 0; j < enemyRenderers.Count; j++)
            {
                Bounds bounds = enemyRenderers[j].bounds;
                float rendererRadius = (bounds.center - prefab.transform.position).magnitude + bounds.extents.magnitude;
                radius = Mathf.Max(radius, rendererRadius);
            }
            eventBoundsRadii[i] = radius;
            enemyRenderers.Clear();
            while (nextEventIndices[i] < data.spawnTimes.Length && data.spawnTimes[nextEventIndices[i]] < elapsed)
                nextEventIndices[i]++;
        }
    }

    private bool ValidatePool(PoolTypeEnums type, bool swarm = false)
    {
        if (ObjectPool.Instance.TryGetPrefab(type, out GameObject prefab) && prefab.GetComponent<EnemyHealth>() != null)
        {
            bool validMovement = swarm
                ? prefab.GetComponent<EnemySwarmMovement>() != null
                : prefab.GetComponent<EnemyDefaultMovement>() != null;
            if (validMovement) return true;
        }
        Debug.LogError($"EnemySpawner: {type} 풀과 적 이동·체력 컴포넌트를 확인해 주세요.", this);
        enabled = false;
        return false;
    }

    private void LateUpdate()
    {
        if (player == null || spawnCamera == null || timeSystem == null ||
            Time.deltaTime <= 0f || Time.timeScale <= 0f) return;

        if (Time.time >= nextOffscreenCheckTime)
        {
            cameraPlanes = cameraView.FrustumPlanes;
            ReturnOffscreenEnemies();
            nextOffscreenCheckTime = Time.time + Mathf.Max(0.01f, offscreenCheckInterval);
        }
        float elapsed = timeSystem.ElapsedSeconds;
        cameraCornersReady = TryGetCameraCorners(out screenCenter);
        UpdateSwarmEvents();
        int index = FindStage(elapsed);
        if (index < 0) return;
        if (currentStage != index)
        {
            currentStage = index;
            spawnTimer = 0f;
        }
        SpawnStage stage = stages[index];
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;
        spawnTimer = stage.spawnInterval;
        int maximum = Mathf.Max(0, stage.maximumEnemies);
        int minimum = Mathf.Clamp(stage.minimumEnemies, 0, maximum);
        int count = minimum - EnemyCrowd.RegularCount;
        for (int i = 0; i < count && EnemyCrowd.RegularCount < maximum; i++)
        {
            if (!TryChooseEnemy(stage, out PoolTypeEnums type)) break;
            if (!ObjectPool.Instance.TryGetPrefab(type, out GameObject prefab)) continue;
            float radius = prefab.GetComponent<EnemyMovement>().BodyRadius;
            if (!TryGetSpawnPosition(radius, out Vector3 position)) break;
            Spawn(position, type);
        }
    }

    private int FindStage(float elapsed)
    {
        int index = -1;
        float latest = float.NegativeInfinity;
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] == null || stages[i].startTime > elapsed || stages[i].startTime < latest) continue;
            latest = stages[i].startTime;
            index = i;
        }
        return index;
    }

    private bool TryChooseEnemy(SpawnStage stage, out PoolTypeEnums type)
    {
        type = PoolTypeEnums.Bat;
        float total = 0f;
        for (int i = 0; i < stage.enemies.Length; i++)
            if (stage.enemies[i] != null) total += Mathf.Max(0f, stage.enemies[i].weight);
        if (total <= 0f) return false;
        float roll = Random.value * total;
        for (int i = 0; i < stage.enemies.Length; i++)
        {
            EnemyEntry entry = stage.enemies[i];
            if (entry == null || entry.weight <= 0f) continue;
            type = entry.poolType;
            roll -= entry.weight;
            if (roll <= 0f) return true;
        }
        return true;
    }

    private GameObject Spawn(Vector3 position, PoolTypeEnums poolType)
    {
        Vector3 direction = player.position - position;
        direction.y = 0f;
        Quaternion rotation = direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity;
        GameObject enemy = ObjectPool.Instance.GetFromPool(poolType, position, rotation);
        if (enemy != null) enemy.GetComponent<EnemyMovement>().SetTarget(player);
        return enemy;
    }

    private void ReturnOffscreenEnemies()
    {
        for (int i = EnemyCrowd.ActiveCount - 1; i >= 0; i--)
        {
            EnemyMovement enemy = EnemyCrowd.GetActiveEnemy(i);
            if (enemy.CheckScreenExit(IsVisible(enemy.gameObject), Time.time))
                ObjectPool.Instance.TryReturnToPool(enemy.gameObject);
        }
    }

    private bool IsVisible(GameObject enemy)
    {
        enemyRenderers.Clear();
        enemy.GetComponentsInChildren<Renderer>(enemyRenderers);
        for (int i = 0; i < enemyRenderers.Count; i++)
        {
            Renderer renderer = enemyRenderers[i];
            if (renderer.enabled && GeometryUtility.TestPlanesAABB(cameraPlanes, renderer.bounds))
                return true;
        }
        return false;
    }

    private void UpdateSwarmEvents()
    {
        float elapsed = timeSystem.ElapsedSeconds;
        for (int i = 0; i < eventEnemies.Count; i++)
        {
            EventEnemyData data = eventEnemies[i];
            while (nextEventIndices[i] < data.spawnTimes.Length && elapsed >= data.spawnTimes[nextEventIndices[i]])
            {
                nextEventIndices[i]++;
                SpawnSwarm(data, eventBoundsRadii[i]);
            }
        }
    }

    private void SpawnSwarm(EventEnemyData data, float boundsRadius)
    {
        int activeCount = 0;
        for (int i = 0; i < EnemyCrowd.ActiveCount; i++)
        {
            if (EnemyCrowd.GetActiveEnemy(i) is EnemySwarmMovement swarm && swarm.EventDataId == data.Id)
                activeCount++;
        }
        int count = Mathf.Min(data.count, Mathf.Max(0, data.maxActive - activeCount));
        Vector3 entryDirection = data.directionMode == EventEnemyDirection.Fixed
            ? data.GetDirection(Vector3.zero, Vector3.zero) : Vector3.zero;
        if (count <= 0 || !TryGetSpawnPosition(boundsRadius, out Vector3 center, entryDirection)) return;
        Vector3 direction = data.GetDirection(center, player.position);
        if (direction.sqrMagnitude <= 0.000001f) return;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        BuildDiamondOffsets(direction, side, count, data.spacing);

        float nearest = float.PositiveInfinity;
        for (int i = 0; i < formationOffsets.Count; i++)
            nearest = Mathf.Min(nearest, Vector3.Dot(formationOffsets[i], spawnEdgeNormal));
        float projection = Vector3.Dot(-direction, spawnEdgeNormal);
        if (projection <= 0.0001f) return;
        float extraDistance = (boundsRadius + data.maxOffset + spawnMargin - nearest) / projection;
        center -= direction * Mathf.Max(0f, extraDistance);

        for (int i = 0; i < formationOffsets.Count; i++)
        {
            Vector3 position = center + formationOffsets[i];
            GameObject enemy = Spawn(position, data.poolType);
            if (enemy == null) continue;
            enemy.GetComponent<EnemySwarmMovement>().ConfigureSwarm(position, direction, data);
        }
    }

    private void BuildDiamondOffsets(Vector3 direction, Vector3 side, int count, float spacing)
    {
        formationOffsets.Clear();
        int rows = 2 * Mathf.CeilToInt(Mathf.Sqrt(count * 0.5f)) - 1;
        int middle = rows / 2;
        int totalWeight = (rows * rows + 1) / 2;
        int[] rowCounts = new int[rows];
        int assigned = 0;
        for (int row = 0; row < rows; row++)
        {
            int weight = 2 * (middle - Mathf.Abs(row - middle)) + 1;
            rowCounts[row] = Mathf.Max(1, Mathf.FloorToInt(count * (float)weight / totalWeight));
            assigned += rowCounts[row];
        }

        int remaining = count - assigned;
        if (remaining % 2 == 1) { rowCounts[middle]++; remaining--; }
        for (int distance = 1; remaining > 0; distance++)
        {
            rowCounts[middle - distance]++;
            rowCounts[middle + distance]++;
            remaining -= 2;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < rowCounts[row]; column++)
            {
                float lateral = (column - (rowCounts[row] - 1) * 0.5f) * spacing;
                float forward = (middle - row) * spacing;
                formationOffsets.Add(side * lateral + direction * forward);
            }
        }
    }

    private bool TryGetCameraCorners(out Vector3 center)
    {
        return cameraView.TryGetCorners(spawnHeight, corners, out center);
    }

    private bool TryGetSpawnPosition(float radius, out Vector3 position, Vector3 entryDirection = default)
    {
        position = Vector3.zero;
        if (!cameraCornersReady) return false;
        int edge = Random.Range(0, 4);
        // 고정 방향은 화면 안으로 진입할 수 있는 쪽의 변에서 생성한다.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Vector3 start = corners[edge];
            Vector3 end = corners[(edge + 1) % 4];
            Vector3 point = Vector3.Lerp(start, end, Random.value);
            Vector3 edgeDirection = end - start;
            Vector3 outward = new Vector3(-edgeDirection.z, 0f, edgeDirection.x).normalized;
            if (Vector3.Dot(outward, point - screenCenter) < 0f) outward = -outward;
            if (entryDirection.sqrMagnitude > 0f && Vector3.Dot(entryDirection, outward) >= -0.0001f)
            {
                edge = (edge + 1) % 4;
                continue;
            }
            Vector3 depthDirection = spawnCamera.orthographic ? spawnCamera.transform.forward : point - spawnCamera.transform.position;
            spawnEdgeNormal = Vector3.Cross(edgeDirection, depthDirection).normalized;
            if (Vector3.Dot(spawnEdgeNormal, outward) < 0f) spawnEdgeNormal = -spawnEdgeNormal;
            position = point + outward * (spawnMargin + radius);
            position.y = spawnHeight;
            return true;
        }
        return false;
    }
}
