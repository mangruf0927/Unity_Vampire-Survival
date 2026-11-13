using UnityEngine;
using Unity.Profiling;

internal class EnemyDefaultMovement
{
    private static readonly ProfilerMarker avoidanceMarker = new("EnemyDefaultMovement.ObstacleAvoidance");
    private static readonly ProfilerMarker moveMarker = new ProfilerMarker("EnemyDefaultMovement.Move");

    private readonly EnemyMovement enemy;
    private float offscreenSince = -1f;
    private bool avoidingObstacle;
    private Vector3 avoidanceDirection;
    private bool canAvoidObstacles;
    private float avoidanceSide;
    private float avoidanceCheckRemaining;
    private float obstacleBlockedTime;

    internal EnemyDefaultMovement(EnemyMovement enemy)
    {
        this.enemy = enemy;
    }

    internal void Reset()
    {
        offscreenSince = -1f;
        ResetObstacleAvoidance();
        avoidanceCheckRemaining = Mathf.Max(0.01f, enemy.AvoidanceCheckInterval) * ((enemy.enemyId & 7) / 8f);
    }

    internal bool ShouldReturnOffscreen(bool inside, float now)
    {
        if (inside)
        {
            offscreenSince = -1f;
            return false;
        }
        if (offscreenSince < 0f) offscreenSince = now;
        return now - offscreenSince >= enemy.OffscreenReturnDelay;
    }

    // 기존 회피 판단을 재사용하고 몸체 충돌과 미끄러짐은 물리 엔진에 맡긴다.
    internal Vector3 GetVelocity(Vector3 direction, Vector3 currentVelocity, float deltaTime, float acceleration)
    {
        using var scope = moveMarker.Auto();
        float remainingDistance = Mathf.Max(0f, direction.magnitude - enemy.MovementRadius - enemy.PlayerRadius);
        if (remainingDistance <= 0.001f)
        {
            ResetObstacleAvoidance();
            return Vector3.zero;
        }
        canAvoidObstacles = true;
        UpdateObstacleAvoidance(direction, deltaTime);
        Vector3 desired = direction.normalized * Mathf.Min(enemy.MoveSpeed, remainingDistance / deltaTime);
        if (avoidingObstacle) desired += avoidanceDirection * (enemy.MoveSpeed * 1.5f);
        desired = Vector3.ClampMagnitude(desired, enemy.MoveSpeed);
        currentVelocity.y = 0f;
        return Vector3.ClampMagnitude(Vector3.MoveTowards(currentVelocity, desired, acceleration * deltaTime), remainingDistance / deltaTime);
    }

    internal void RecordObstacleMovement(Vector3 requested, Vector3 actual, float deltaTime)
    {
        if (!canAvoidObstacles) return;
        // 다른 적에게 옆으로 밀린 거리도 전진한 것으로 세면 막힘을 놓친다.
        bool blocked = requested.sqrMagnitude > 0.00000001f
            && Vector3.Dot(actual, requested) < requested.sqrMagnitude * 0.5f;
        obstacleBlockedTime = blocked ? obstacleBlockedTime + deltaTime : 0f;
    }

    private void UpdateObstacleAvoidance(Vector3 targetDirection, float deltaTime)
    {
        avoidanceCheckRemaining -= deltaTime;
        if (!avoidingObstacle && obstacleBlockedTime < 0.15f) return;
        if (avoidanceCheckRemaining > 0f) return;
        avoidanceCheckRemaining = Mathf.Max(0.01f, enemy.AvoidanceCheckInterval);
        using var scope = avoidanceMarker.Auto();

        float distance = Mathf.Max(0.1f, enemy.ObstacleLookAhead);
        Vector3 probe = Vector3.ClampMagnitude(targetDirection, distance);
        if (!enemy.CastObstacle(probe, out RaycastHit hit))
        {
            ResetObstacleAvoidance();
            return;
        }

        Vector3 normal = hit.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude <= 0.00001f) return;
        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        float side = avoidingObstacle ? avoidanceSide : enemy.sidePreference;
        Vector3 first = (tangent * (side * distance) + normal * enemy.ObstacleClearance).normalized;
        Vector3 second = (tangent * (-side * distance) + normal * enemy.ObstacleClearance).normalized;
        float firstSpace = GetFreeDistance(first, distance);
        float secondSpace = GetFreeDistance(second, distance);

        bool changeSide = !avoidingObstacle ? secondSpace > firstSpace + 0.05f : obstacleBlockedTime >= 0.5f && firstSpace < 0.05f && secondSpace > 0.1f;
        if (changeSide)
        {
            side = -side;
            first = second;
            firstSpace = secondSpace;
            obstacleBlockedTime = 0f;
        }
        avoidanceSide = side;
        avoidingObstacle = true;

        avoidanceDirection = firstSpace > 0.02f ? first : Vector3.zero;
    }

    private float GetFreeDistance(Vector3 direction, float distance)
    {
        return enemy.CastObstacle(direction * distance, out RaycastHit hit)
            ? hit.distance : distance;
    }

    internal void ResetObstacleAvoidance()
    {
        avoidingObstacle = false;
        avoidanceDirection = Vector3.zero;
        obstacleBlockedTime = 0f;
        avoidanceCheckRemaining = 0f;
        canAvoidObstacles = false;
    }

}
