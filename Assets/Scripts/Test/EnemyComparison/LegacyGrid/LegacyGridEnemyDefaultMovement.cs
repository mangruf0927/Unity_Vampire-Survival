// 비교 테스트 전용: Collider 전환 이전 격자 이동을 보관한다. 실제 게임에서는 사용하지 않는다.
using UnityEngine;

internal class LegacyGridEnemyDefaultMovement
{

    private readonly LegacyGridEnemyMovement enemy;
    private float offscreenSince = -1f;
    private bool avoidingObstacle;
    private Vector3 avoidanceDirection;
    private bool canAvoidObstacles;
    private float avoidanceSide;
    private float avoidanceCheckRemaining;
    private float obstacleBlockedTime;

    internal LegacyGridEnemyDefaultMovement(LegacyGridEnemyMovement enemy)
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

    internal void Move(Vector3 direction, float stepDeltaTime)
    {

        canAvoidObstacles = false;
        if (enemy.TryGetKnockbackMovement(stepDeltaTime, out Vector3 knockback))
        {
            enemy.nextPosition = enemy.framePosition + knockback;
            enemy.velocity = Vector3.zero;
            return;
        }

        float remainingDistance = Mathf.Max(0f, direction.magnitude - enemy.BodyRadius - enemy.PlayerRadius);
        if (remainingDistance <= 0.001f)
        {
            ResetObstacleAvoidance();
            enemy.velocity = Vector3.zero;
            enemy.nextPosition = enemy.framePosition;
            return;
        }

        // 넉백·접촉 정지 분기를 통과한 적만 주변 분리 계산이 필요하다.
        Vector3 separation = LegacyGridEnemyCrowd.GetSeparation(enemy);
        canAvoidObstacles = true;
        UpdateObstacleAvoidance(direction, stepDeltaTime);
        float chaseSpeed = Mathf.Min(enemy.MoveSpeed, remainingDistance / stepDeltaTime);
        Vector3 desiredVelocity = direction.normalized * chaseSpeed + separation * (enemy.SeparationStrength * enemy.MoveSpeed);
        if (avoidingObstacle) desiredVelocity += avoidanceDirection * (enemy.MoveSpeed * 1.5f);
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, enemy.MoveSpeed);
        enemy.velocity = Vector3.MoveTowards(enemy.velocity, desiredVelocity, enemy.MoveSpeed * 8f * stepDeltaTime);

        Vector3 movement = Vector3.ClampMagnitude(enemy.velocity * stepDeltaTime, remainingDistance);
        enemy.nextPosition = enemy.framePosition + movement;
    }

    // 기존 회피 판단을 재사용하고 몸체 충돌과 미끄러짐은 물리 엔진에 맡긴다.
    internal Vector3 GetPhysicsVelocity(Vector3 direction, Vector3 currentVelocity, float deltaTime, float acceleration)
    {
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
