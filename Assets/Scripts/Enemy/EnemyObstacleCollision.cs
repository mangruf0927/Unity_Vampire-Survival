using UnityEngine;

public static class EnemyObstacleCollision
{

    private const int ObstacleMask = 1 << 11;
    private const float Skin = 0.002f;

    internal static bool CastSphere(Vector3 start, Vector3 displacement, float radius, out RaycastHit hit)
    {
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
