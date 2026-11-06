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

    [SerializeField, Min(0)] private int maximumSwarmEnemies = 120;

    [SerializeField, Min(1)] private int swarmCount = 20;
    [SerializeField, Min(0.1f)] private float swarmSpeed = 16f;
    [SerializeField, Min(1f)] private float swarmPushStrength = 5f;
    [SerializeField, Min(1.1f)] private float swarmSpacing = 1.3f;
    [SerializeField, Min(0f)] private float swarmMaxOffset = 0.25f;
    [SerializeField, Min(0f)] private float swarmRecoverySpeed = 4f;
    private readonly int[] swarmTimes = { 125, 130, 135, 425, 430, 435 };
    private int nextSwarmEvent;

    private readonly List<Vector3> formationOffsets = new();
    private readonly List<Renderer> enemyRenderers = new();
    private readonly Plane[] cameraPlanes = new Plane[6];

    private float spawnTimer;
    private int currentStage = -1;
    [SerializeField] private float spawnHeight = 1f;

    private readonly Vector3[] corners = new Vector3[4];
    private bool cameraCornersReady;
    private Vector3 screenCenter;
    private Vector3 spawnEdgeNormal;
    private float swarmBoundsRadius;

    private void Start()
    {
        if (player == null || spawnCamera == null || timeSystem == null ||
            stages == null || stages.Length != 10)
        {
            Debug.LogError("EnemySpawner: Player, Camera, Time System과 10개 구간을 설정해 주세요.", this);
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
        if (!ValidatePool(PoolTypeEnums.Bat)) return;
        ObjectPool.Instance.TryGetPrefab(PoolTypeEnums.Bat, out GameObject swarmPrefab);
        swarmBoundsRadius = swarmPrefab.GetComponent<EnemyMovement>().BodyRadius;

        swarmPrefab.GetComponentsInChildren<Renderer>(enemyRenderers);
        for (int i = 0; i < enemyRenderers.Count; i++)
        {
            Bounds bounds = enemyRenderers[i].bounds;
            float radius = (bounds.center - swarmPrefab.transform.position).magnitude + bounds.extents.magnitude;
            swarmBoundsRadius = Mathf.Max(swarmBoundsRadius, radius);
        }
        enemyRenderers.Clear();

        float elapsed = timeSystem.ElapsedSeconds;
        while (nextSwarmEvent < swarmTimes.Length && swarmTimes[nextSwarmEvent] < elapsed) nextSwarmEvent++;
    }

    private bool ValidatePool(PoolTypeEnums type)
    {
        if (ObjectPool.Instance.TryGetPrefab(type, out GameObject prefab) && prefab.GetComponent<EnemyMovement>() != null && prefab.GetComponent<EnemyHealth>() != null) return true;
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
            GeometryUtility.CalculateFrustumPlanes(spawnCamera, cameraPlanes);
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
            if (enemy.ShouldReturnOffscreen(IsVisible(enemy.gameObject), Time.time))
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
        while (nextSwarmEvent < swarmTimes.Length && elapsed >= swarmTimes[nextSwarmEvent])
        {
            nextSwarmEvent++;
            SpawnSwarm();
        }
    }

    private void SpawnSwarm()
    {
        int count = Mathf.Min(swarmCount, Mathf.Max(0, maximumSwarmEnemies - EnemyCrowd.ActiveSwarmCount));
        if (count <= 0 || !TryGetSpawnPosition(swarmBoundsRadius, out Vector3 center)) return;
        Vector3 direction = player.position - center;
        direction.y = 0f;
        direction.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        BuildDiamondOffsets(direction, side, count);

        float nearest = float.PositiveInfinity;
        for (int i = 0; i < formationOffsets.Count; i++)
            nearest = Mathf.Min(nearest, Vector3.Dot(formationOffsets[i], spawnEdgeNormal));
        float projection = Vector3.Dot(-direction, spawnEdgeNormal);
        if (projection <= 0.0001f) return;
        float extraDistance = (swarmBoundsRadius + swarmMaxOffset + spawnMargin - nearest) / projection;
        center -= direction * Mathf.Max(0f, extraDistance);

        for (int i = 0; i < formationOffsets.Count; i++)
        {
            Vector3 position = center + formationOffsets[i];
            GameObject bat = Spawn(position, PoolTypeEnums.Bat);
            if (bat == null) continue;
            bat.GetComponent<EnemyMovement>().ConfigureSwarm(position, direction, swarmSpeed,
                swarmPushStrength, swarmMaxOffset, swarmRecoverySpeed);
        }
    }

    private void BuildDiamondOffsets(Vector3 direction, Vector3 side, int count)
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
                float lateral = (column - (rowCounts[row] - 1) * 0.5f) * swarmSpacing;
                float forward = (middle - row) * swarmSpacing;
                formationOffsets.Add(side * lateral + direction * forward);
            }
        }
    }

    private bool TryGetCameraCorners(out Vector3 center)
    {
        center = Vector3.zero;
        Plane spawnPlane = new(Vector3.up, new Vector3(0f, spawnHeight, 0f));
        for (int i = 0; i < 4; i++)
        {
            float x = i == 1 || i == 2 ? 1f : 0f;
            float y = i >= 2 ? 1f : 0f;
            Ray ray = spawnCamera.ViewportPointToRay(new Vector3(x, y, 0f));
            if (!spawnPlane.Raycast(ray, out float distance)) return false;
            corners[i] = ray.GetPoint(distance);
            center += corners[i] * 0.25f;
        }
        return true;
    }

    private bool TryGetSpawnPosition(float radius, out Vector3 position)
    {
        position = Vector3.zero;
        if (!cameraCornersReady) return false;
        int edge = Random.Range(0, 4);
        Vector3 start = corners[edge];
        Vector3 end = corners[(edge + 1) % 4];
        Vector3 point = Vector3.Lerp(start, end, Random.value);
        Vector3 edgeDirection = end - start;
        Vector3 outward = new Vector3(-edgeDirection.z, 0f, edgeDirection.x).normalized;
        if (Vector3.Dot(outward, point - screenCenter) < 0f) outward = -outward;
        Vector3 depthDirection = spawnCamera.orthographic ? spawnCamera.transform.forward : point - spawnCamera.transform.position;
        spawnEdgeNormal = Vector3.Cross(edgeDirection, depthDirection).normalized;
        if (Vector3.Dot(spawnEdgeNormal, outward) < 0f) spawnEdgeNormal = -spawnEdgeNormal;
        position = point + outward * (spawnMargin + radius);
        position.y = spawnHeight;
        return true;
    }
}
