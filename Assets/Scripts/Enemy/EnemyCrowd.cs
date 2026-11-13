using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using UnityEngine.SceneManagement;

public static class EnemyCrowd
{
    private static readonly ProfilerMarker refreshGridMarker = new("EnemyCrowd.RefreshGrid");
    private static readonly ProfilerMarker tryGetProjectileHitMarker = new("EnemyCrowd.TryGetProjectileHit");

    private static readonly ProfilerMarker swarmStepMarker = new("EnemyCrowd.EventStep");
    private static readonly ProfilerMarker swarmOverlapMarker = new("EnemyCrowd.EventOverlap");
    private static float swarmStepTime = float.NegativeInfinity;
    private const int SwarmCorrectionPasses = 2;

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
        swarmStepTime = float.NegativeInfinity;
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

    // 일반 적끼리의 충돌은 Physics에 맡기고 이벤트-일반 쌍만 기존 수식으로 보정한다.
    internal static void UpdateSwarms()
    {
        if (ActiveSwarmCount == 0 || IsFrozen || Time.deltaTime <= 0f || swarmStepTime == Time.fixedTime) return;
        using var scope = swarmStepMarker.Auto();
        swarmStepTime = Time.fixedTime;
        float deltaTime = Time.fixedDeltaTime;
        for (int i = 0; i < activeEnemies.Count; i++) activeEnemies[i].PrepareSwarmStep(deltaTime);

        for (int pass = 0; pass < SwarmCorrectionPasses; pass++)
        {
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                activeEnemies[i].crowdCorrection = Vector3.zero;
                activeEnemies[i].crowdContacts = 0;
            }
            RefreshGrid(true);
            for (int i = 0; i < activeEnemies.Count; i++)
                if (activeEnemies[i].IsSwarm) CorrectSwarmOverlap(activeEnemies[i]);

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyMovement enemy = activeEnemies[i];
                if (enemy.crowdContacts == 0) continue;
                // 일반 적의 밀림 상한은 반복 횟수와 무관하게 한 스텝 기준으로 유지한다.
                float correctionTime = enemy.IsSwarm ? deltaTime : deltaTime / SwarmCorrectionPasses;
                Vector3 movement = Vector3.ClampMagnitude(enemy.crowdCorrection / enemy.crowdContacts,
                    enemy.CorrectionSpeed * correctionTime);
                if (!enemy.IsSwarm && EnemyObstacleCollision.CastSphere(enemy.crowdPosition, movement,
                    enemy.CrowdRadius, out RaycastHit hit))
                    movement = Vector3.ClampMagnitude(movement, Mathf.Max(0f, hit.distance - 0.002f));
                enemy.crowdPosition += movement;
            }
            // 매 보정 후 기존 최대 경로 이탈 제한을 다시 적용한다.
            for (int i = 0; i < activeEnemies.Count; i++) activeEnemies[i].ApplyCrowdStep();
        }
        hitGridFrame = -1;
    }

    private static void CorrectSwarmOverlap(EnemyMovement swarm)
    {
        using var scope = swarmOverlapMarker.Auto();
        Vector2Int cell = GetCell(swarm.crowdPosition);
        int cells = Mathf.CeilToInt((swarm.CrowdRadius + largestRadius) / CellSize);
        for (int x = -cells; x <= cells; x++)
        {
            for (int z = -cells; z <= cells; z++)
            {
                grid.TryGetValue(cell + new Vector2Int(x, z), out EnemyMovement other);
                while (other != null)
                {
                    if (!other.IsSwarm)
                    {
                        Vector3 away = swarm.crowdPosition - other.crowdPosition;
                        away.y = 0f;
                        float radius = swarm.CrowdRadius + other.CrowdRadius;
                        if (away.sqrMagnitude < radius * radius)
                        {
                            float distance = away.magnitude;
                            float overlap = radius - distance - 0.001f;
                            if (overlap > 0f)
                            {
                                // 중심이 같으면 기존처럼 개체 ID 쌍으로 결정한 분리 방향을 사용한다.
                                Vector3 normal;
                                if (distance > 0.0001f) normal = away / distance;
                                else
                                {
                                    int pair = swarm.enemyId ^ other.enemyId;
                                    float angle = (pair & 1023) * (Mathf.PI * 2f / 1024f);
                                    normal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                                    if (swarm.enemyId > other.enemyId) normal = -normal;
                                }
                                float totalWeight = swarm.CorrectionWeight + other.CorrectionWeight;
                                Vector3 offset = normal * overlap;
                                swarm.crowdCorrection += offset * (swarm.CorrectionWeight / totalWeight);
                                swarm.crowdContacts++;

                                // 통과 전후의 방사형 보정이 서로 상쇄되지 않도록 일반 적 쪽만 제한한다.
                                // 측면 밀림은 유지하고 박쥐 진행 방향의 반대 성분만 제거한다.
                                Vector3 pushDirection = -normal;
                                float forward = Vector3.Dot(pushDirection, swarm.SwarmDirection);
                                if (forward < 0f) pushDirection -= swarm.SwarmDirection * forward;
                                if (pushDirection.sqrMagnitude > 0.00000001f)
                                {
                                    other.crowdCorrection += pushDirection * overlap * (other.CorrectionWeight / totalWeight);
                                    other.crowdContacts++;
                                }
                            }
                        }
                    }
                    other = other.nextInCell;
                }
            }
        }
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

    private static void RefreshGrid(bool useCrowdPosition = false)
    {
        using var scope = refreshGridMarker.Auto();

        grid.Clear();
        largestRadius = 0f;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];
            Vector2Int cell = GetCell(useCrowdPosition ? enemy.crowdPosition : enemy.transform.position);

            grid.TryGetValue(cell, out enemy.nextInCell);
            grid[cell] = enemy;
            largestRadius = Mathf.Max(largestRadius, useCrowdPosition ? enemy.CrowdRadius : enemy.BodyRadius);
        }
    }

}
