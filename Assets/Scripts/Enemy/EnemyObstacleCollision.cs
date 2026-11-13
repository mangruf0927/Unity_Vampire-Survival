using UnityEngine;
using Unity.Profiling;

// 물리 적의 회피 및 넉백에 필요한 장애물 감지를 제공한다.
public static class EnemyObstacleCollision
{
    private static readonly ProfilerMarker castMarker = new ProfilerMarker("EnemyObstacleCollision.Cast");

    private const int ObstacleMask = 1 << 11;
    private const float Skin = 0.002f;

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

}
