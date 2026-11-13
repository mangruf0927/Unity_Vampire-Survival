using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;

public static class EnemyCrowd
{
    private static readonly ProfilerMarker refreshGridMarker = new("EnemyCrowd.RefreshGrid");
    private static readonly ProfilerMarker tryGetProjectileHitMarker = new("EnemyCrowd.TryGetProjectileHit");

    private const float CellSize = 2f;

    private static readonly List<EnemyMovement> activeEnemies = new();
    private static readonly Dictionary<Vector2Int, EnemyMovement> grid = new();

    internal static int ActiveCount => activeEnemies.Count;
    internal static int ActiveSwarmCount { get; private set; }
    internal static int RegularCount => ActiveCount - ActiveSwarmCount;
    internal static void RegisterSwarm() => ActiveSwarmCount++;
    internal static EnemyMovement GetActiveEnemy(int index) => activeEnemies[index];

    private static int hitGridFrame = -1;
    private static float largestRadius;
    private static float frozenUntil;
    private static int frozenSceneHandle;
    internal static bool IsFrozen => Time.time < frozenUntil && SceneManager.GetActiveScene().handle == frozenSceneHandle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCrowd()
    {
        activeEnemies.Clear();
        ActiveSwarmCount = 0;
        grid.Clear();
        hitGridFrame = -1;
        frozenUntil = 0f;
        frozenSceneHandle = 0;
    }

    public static void Register(EnemyMovement enemy)
    {
        activeEnemies.Add(enemy);
        hitGridFrame = -1;
    }

    public static void Unregister(EnemyMovement enemy)
    {
        if (activeEnemies.Remove(enemy) && enemy.IsSwarm) ActiveSwarmCount--;
        hitGridFrame = -1;
    }

    public static void FreezeFor(float duration)
    {
        if (duration <= 0f) return;
        frozenSceneHandle = SceneManager.GetActiveScene().handle;
        frozenUntil = Time.time + duration;
        for (int i = 0; i < activeEnemies.Count; i++) activeEnemies[i].ClearFrozenMotion();
    }

    internal static bool TryGetProjectileHit(Vector3 start, Vector3 end, float hitRadius, out EnemyMovement hitEnemy, HashSet<EnemyMovement> ignoredEnemies = null)
    {
        return TryGetProjectileHit(start, end, hitRadius, out hitEnemy, out _, ignoredEnemies);
    }

    internal static bool TryGetProjectileHit(Vector3 start, Vector3 end, float hitRadius, out EnemyMovement hitEnemy, out float firstHitTime, HashSet<EnemyMovement> ignoredEnemies = null)
    {
        using var scope = tryGetProjectileHitMarker.Auto();

        hitEnemy = null;
        firstHitTime = float.PositiveInfinity;
        if (hitGridFrame != Time.frameCount)
        {
            RefreshGrid();
            hitGridFrame = Time.frameCount;
        }

        if (grid.Count == 0) return false;

        float padding = largestRadius + hitRadius;
        Vector2Int min = GetCell(new Vector3(Mathf.Min(start.x, end.x) - padding, 0f, Mathf.Min(start.z, end.z) - padding));
        Vector2Int max = GetCell(new Vector3(Mathf.Max(start.x, end.x) + padding, 0f, Mathf.Max(start.z, end.z) + padding));

        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.y; z <= max.y; z++)
            {
                grid.TryGetValue(new Vector2Int(x, z), out EnemyMovement enemy);
                while (enemy != null)
                {
                    if (enemy.isActiveAndEnabled && (ignoredEnemies == null || !ignoredEnemies.Contains(enemy)))
                    {
                        Vector3 center = enemy.transform.position;
                        float t = lengthSquared > 0f
                            ? Mathf.Clamp01(Vector3.Dot(center - start, segment) / lengthSquared)
                            : 0f;
                        Vector3 closestPoint = start + segment * t;
                        float radius = enemy.BodyRadius + hitRadius;
                        if ((center - closestPoint).sqrMagnitude <= radius * radius)
                        {
                            // 여러 적을 통과하는 프레임에도 격자 순서가 아닌 첫 접촉 대상을 선택한다.
                            float hitTime = 0f;
                            if (lengthSquared > 0f)
                            {
                                Vector3 offset = start - center;
                                float b = Vector3.Dot(offset, segment);
                                float c = offset.sqrMagnitude - radius * radius;
                                float discriminant = Mathf.Max(0f, b * b - lengthSquared * c);
                                hitTime = Mathf.Max(0f, (-b - Mathf.Sqrt(discriminant)) / lengthSquared);
                            }
                            if (hitTime < firstHitTime)
                            {
                                firstHitTime = hitTime;
                                hitEnemy = enemy;
                            }
                        }
                    }
                    enemy = enemy.nextInCell;
                }
            }
        }
        return hitEnemy != null;
    }

    private static Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));
    }

    private static void RefreshGrid()
    {
        using var scope = refreshGridMarker.Auto();

        grid.Clear();
        largestRadius = 0f;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];
            Vector2Int cell = GetCell(enemy.transform.position);

            grid.TryGetValue(cell, out enemy.nextInCell);
            grid[cell] = enemy;
            largestRadius = Mathf.Max(largestRadius, enemy.BodyRadius);
        }
    }

}
