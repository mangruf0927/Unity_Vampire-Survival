using System.Collections.Generic;
using UnityEngine;

public static class EnemyCrowd
{
    private const float CellSize = 2f;
    private const int CorrectionPasses = 4;
    private const float ContactTolerance = 0.001f;

    private static readonly List<EnemyMovement> activeEnemies = new();
    private static readonly Dictionary<Vector2Int, EnemyMovement> grid = new();

    private static int simulationFrame = -1;
    private static int hitGridFrame = -1;
    private static float largestRadius;
    private static float stepDeltaTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCrowd()
    {
        activeEnemies.Clear();
        grid.Clear();
        simulationFrame = -1;
        hitGridFrame = -1;
    }

    public static void Register(EnemyMovement enemy)
    {
        activeEnemies.Add(enemy);
        hitGridFrame = -1;
    }

    public static void Unregister(EnemyMovement enemy)
    {
        activeEnemies.Remove(enemy);
        hitGridFrame = -1;
    }

    public static void UpdateCrowd()
    {
        if (Time.deltaTime <= 0f || simulationFrame == Time.frameCount) return;

        simulationFrame = Time.frameCount;

        int steps = Mathf.Clamp(Mathf.CeilToInt(Time.deltaTime / (1f / 60f)), 1, 4);
        stepDeltaTime = Time.deltaTime / steps;

        for (int i = 0; i < steps; i++) SimulateCrowd();

        RefreshGrid(true);
        hitGridFrame = Time.frameCount;
    }

    internal static bool HasProjectileHit(Vector3 start, Vector3 end, float hitRadius)
    {
        if (hitGridFrame != Time.frameCount)
        {
            RefreshGrid(true);
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
                    if (enemy.isActiveAndEnabled)
                    {
                        Vector3 center = enemy.transform.position;
                        float t = lengthSquared > 0f
                            ? Mathf.Clamp01(Vector3.Dot(center - start, segment) / lengthSquared)
                            : 0f;
                        Vector3 closestPoint = start + segment * t;
                        float radius = enemy.BodyRadius + hitRadius;
                        if ((center - closestPoint).sqrMagnitude <= radius * radius) return true;
                    }
                    enemy = enemy.nextInCell;
                }
            }
        }
        return false;
    }

    private static void SimulateCrowd()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];
            enemy.framePosition = enemy.transform.position;
            enemy.nextPosition = enemy.framePosition;

            if (enemy.Target != null) enemy.targetPosition = enemy.Target.position;
        }

        RefreshGrid();

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];

            if (enemy.Target != null)
                enemy.Move(enemy.GetTargetDirection(), GetSeparation(enemy), stepDeltaTime);
            else
                enemy.velocity = Vector3.zero;
        }

        for (int pass = 0; pass < CorrectionPasses; pass++)
        {
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyMovement enemy = activeEnemies[i];
                enemy.correction = Vector3.zero;
                enemy.contactCount = 0;
            }

            RefreshGrid();

            for (int i = 0; i < activeEnemies.Count; i++)
                CorrectOverlap(activeEnemies[i]);

            for (int i = 0; i < activeEnemies.Count; i++)
            {
                EnemyMovement enemy = activeEnemies[i];
                if (enemy.Target == null) continue;

                if (enemy.contactCount > 0)
                {
                    Vector3 offset = enemy.correction / enemy.contactCount;
                    enemy.nextPosition += Vector3.ClampMagnitude(offset, enemy.OverlapCorrectionSpeed * stepDeltaTime);
                }

            }
        }

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];
            if (enemy.Target == null) continue;

            enemy.transform.position = enemy.nextPosition;

            Vector3 facingDirection = enemy.targetPosition - enemy.nextPosition;
            facingDirection.y = 0f;

            if (facingDirection.sqrMagnitude > 0.000001f) enemy.Rotate(facingDirection, stepDeltaTime);
        }
    }

    private static Vector2Int GetCell(Vector3 position)
    {
        return new Vector2Int(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));
    }

    private static void RefreshGrid(bool useCurrentPosition = false)
    {
        grid.Clear();
        largestRadius = 0f;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];
            Vector2Int cell = GetCell(useCurrentPosition ? enemy.transform.position : enemy.nextPosition);

            grid.TryGetValue(cell, out enemy.nextInCell);
            grid[cell] = enemy;
            largestRadius = Mathf.Max(largestRadius, enemy.BodyRadius);
        }
    }

    private static Vector3 GetSeparation(EnemyMovement enemy)
    {
        Vector3 separation = Vector3.zero;
        Vector3 forward = enemy.GetTargetDirection().normalized;
        Vector2Int cell = GetCell(enemy.framePosition);
        int cells = Mathf.CeilToInt(Mathf.Max(enemy.SeparationRange, enemy.BodyRadius + largestRadius) / CellSize);

        for (int x = -cells; x <= cells; x++)
        {
            for (int z = -cells; z <= cells; z++)
            {
                grid.TryGetValue(cell + new Vector2Int(x, z), out EnemyMovement other);

                while (other != null)
                {
                    if (other != enemy)
                    {
                        Vector3 away = enemy.framePosition - other.framePosition;
                        away.y = 0f;

                        float distance = away.magnitude;
                        float range = Mathf.Max(enemy.SeparationRange, enemy.BodyRadius + other.BodyRadius);

                        if (distance < range)
                        {
                            Vector3 normal = distance > 0.0001f ? away / distance : enemy.GetOverlapDirection(other);
                            float weight = 1f - distance / range;

                            separation += normal * weight * weight;

                            float blocking = Mathf.Max(0f, Vector3.Dot(-normal, forward));
                            separation += 0.5f * blocking * enemy.sidePreference * weight * weight * Vector3.Cross(Vector3.up, forward);
                        }
                    }

                    other = other.nextInCell;
                }
            }
        }

        return Vector3.ClampMagnitude(separation, 1f);
    }

    private static void CorrectOverlap(EnemyMovement enemy)
    {
        Vector2Int cell = GetCell(enemy.nextPosition);
        int cells = Mathf.CeilToInt((enemy.BodyRadius + largestRadius) / CellSize);

        for (int x = -cells; x <= cells; x++)
        {
            for (int z = -cells; z <= cells; z++)
            {
                grid.TryGetValue(cell + new Vector2Int(x, z), out EnemyMovement other);

                while (other != null)
                {
                    if (enemy.enemyId < other.enemyId && (enemy.Target != null || other.Target != null))
                    {
                        Vector3 away = enemy.nextPosition - other.nextPosition;
                        away.y = 0f;

                        float distance = away.magnitude;
                        float overlap = enemy.BodyRadius + other.BodyRadius - distance - ContactTolerance;

                        if (overlap > 0f)
                        {
                            Vector3 normal = distance > 0.0001f ? away / distance : enemy.GetOverlapDirection(other);
                            float share = enemy.Target != null && other.Target != null ? 0.5f : 1f;
                            Vector3 offset = normal * overlap * share;

                            if (enemy.Target != null)
                            {
                                enemy.correction += offset;
                                enemy.contactCount++;
                            }

                            if (other.Target != null)
                            {
                                other.correction -= offset;
                                other.contactCount++;
                            }
                        }
                    }

                    other = other.nextInCell;
                }
            }
        }
    }
}
