// 비교 테스트 전용: Collider 전환 이전 격자 이동을 보관한다. 실제 게임에서는 사용하지 않는다.
using UnityEngine;
using Unity.Profiling;

// 기존 격자 이동의 충돌 처리와 물리 이동의 장애물 감지를 제공한다.
public static class LegacyGridEnemyObstacleCollision
{
    private static readonly ProfilerMarker castMarker = new ProfilerMarker("LegacyGridEnemyObstacleCollision.Cast");
    private static readonly ProfilerMarker resolveOverlapMarker = new ProfilerMarker("LegacyGridEnemyObstacleCollision.ResolveOverlap");
    private static readonly ProfilerMarker moveMarker = new ProfilerMarker("LegacyGridEnemyObstacleCollision.Move");

    private const int ObstacleMask = 1 << 11;
    private const float Skin = 0.002f;
    private const float HalfSegmentHeight = 0.5f;
    private const int MaximumPasses = 3;
    private static readonly Collider[] overlaps = new Collider[32];

    // Rigidbody 적은 실제 SphereCollider와 같은 형태로 감지한다.
    internal static bool CastSphere(Vector3 start, Vector3 displacement, float radius, out RaycastHit hit)
    {
        using var scope = castMarker.Auto();
        float distance = displacement.magnitude;
        if (distance <= 0.00001f)
        {
            hit = default;
            return false;
        }
        return Physics.SphereCast(start, radius, displacement / distance, out hit,
            distance + Skin, ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public static bool Cast(Vector3 start, Vector3 displacement, float radius, out RaycastHit hit)
    {
        using var scope = castMarker.Auto();

        float distance = displacement.magnitude;
        if (distance <= 0.00001f)
        {
            hit = default;
            return false;
        }
        return Physics.CapsuleCast(start + Vector3.down * HalfSegmentHeight,
            start + Vector3.up * HalfSegmentHeight, radius, displacement / distance, out hit,
            distance + Skin, ObstacleMask, QueryTriggerInteraction.Ignore);
    }

    public static Vector3 ResolveOverlap(Vector3 position, float radius)
    {
        using var scope = resolveOverlapMarker.Auto();

        for (int pass = 0; pass < MaximumPasses; pass++)
        {
            int count = Physics.OverlapCapsuleNonAlloc(position + Vector3.down * HalfSegmentHeight,
                position + Vector3.up * HalfSegmentHeight, radius + Skin, overlaps,
                ObstacleMask, QueryTriggerInteraction.Ignore);
            if (count == 0) break;

            // 버퍼가 가득 찬 극단적인 밀집에서도 장애물을 누락하지 않는다.
            Collider[] candidates = overlaps;
            if (count == overlaps.Length)
            {
                candidates = Physics.OverlapCapsule(position + Vector3.down * HalfSegmentHeight,
                    position + Vector3.up * HalfSegmentHeight, radius + Skin,
                    ObstacleMask, QueryTriggerInteraction.Ignore);
                count = candidates.Length;
            }
            bool moved = false;
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = candidates[i];
                Bounds bounds = obstacle.bounds;
                Vector3 point = position;
                point.y = Mathf.Clamp(bounds.center.y, position.y - HalfSegmentHeight, position.y + HalfSegmentHeight);
                Vector3 away = point - obstacle.ClosestPoint(point);
                away.y = 0f;
                float distance = away.magnitude;
                float penetration = radius + Skin - distance;
                if (penetration <= 0f) continue;

                if (distance > 0.00001f)
                    position += away * (penetration / distance);
                else
                {
                    // 중심이 BoxCollider 안에 있으면 가장 가까운 XZ 면으로 뺀다.
                    float left = position.x - bounds.min.x + radius + Skin;
                    float right = bounds.max.x - position.x + radius + Skin;
                    float back = position.z - bounds.min.z + radius + Skin;
                    float forward = bounds.max.z - position.z + radius + Skin;
                    float minimum = Mathf.Min(Mathf.Min(left, right), Mathf.Min(back, forward));
                    if (minimum == left) position.x -= left;
                    else if (minimum == right) position.x += right;
                    else if (minimum == back) position.z -= back;
                    else position.z += forward;
                }
                moved = true;
            }
            if (!moved) break;
        }
        return position;
    }

    public static Vector3 Move(Vector3 start, Vector3 displacement, float radius)
    {
        using var scope = moveMarker.Auto();

        displacement.y = 0f;
        Vector3 position = start;
        for (int pass = 0; pass < MaximumPasses; pass++)
        {
            float distance = displacement.magnitude;
            if (distance <= 0.00001f) break;
            Vector3 direction = displacement / distance;
            if (!Physics.CapsuleCast(position + Vector3.down * HalfSegmentHeight,
                position + Vector3.up * HalfSegmentHeight, radius, direction, out RaycastHit hit,
                distance + Skin, ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                position += displacement;
                break;
            }

            float travel = Mathf.Clamp(hit.distance - Skin, 0f, distance);
            position += direction * travel;
            displacement -= direction * travel;
            Vector3 normal = hit.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude <= 0.00001f) break;
            normal.Normalize();
            float inward = Vector3.Dot(displacement, normal);
            if (inward >= 0f) break;
            displacement -= normal * inward;
        }
        return position;
    }
}
