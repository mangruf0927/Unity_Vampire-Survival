using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 360f;
    [SerializeField, Min(0.01f)] private float bodyRadius = 0.5f;
    [SerializeField, Min(0f)] private float playerRadius = 0.5f;
    [SerializeField, Min(0f)] private float separationRange = 1.5f;
    [SerializeField, Min(0f)] private float separationStrength = 2f;
    [SerializeField, Min(0f)] private float overlapCorrectionSpeed = 3f;

    // EnemyCrowd와 공유하는 실행 중 상태이며 인스펙터 설정은 아니다.
    internal EnemyMovement nextInCell;
    internal Vector3 framePosition;
    internal Vector3 nextPosition;
    internal Vector3 targetPosition;
    internal Vector3 velocity;
    internal Vector3 correction;

    internal int contactCount;
    internal int enemyId;
    internal float sidePreference;

    internal Transform Target => target;
    internal float BodyRadius => bodyRadius;
    internal float SeparationRange => separationRange;
    internal float OverlapCorrectionSpeed => overlapCorrectionSpeed;

    private void OnEnable()
    {
        enemyId = GetInstanceID();
        sidePreference = ((enemyId >> 2) & 1) == 0 ? 1f : -1f;
        velocity = Vector3.zero;
        EnemyCrowd.Register(this);
    }

    private void OnDisable()
    {
        EnemyCrowd.Unregister(this);
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        EnemyCrowd.UpdateCrowd();
    }

    internal Vector3 GetTargetDirection()
    {
        Vector3 direction = targetPosition - framePosition;
        direction.y = 0f;
        return direction;
    }

    internal void Move(Vector3 direction, Vector3 separation, float stepDeltaTime)
    {
        Vector3 desiredVelocity = Vector3.ClampMagnitude(direction.normalized + separation * separationStrength, 1f) * moveSpeed;
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, moveSpeed * 8f * stepDeltaTime);
        nextPosition = framePosition + velocity * stepDeltaTime;
    }

    internal void LimitApproach()
    {
        if (target == null) return;

        Vector3 outward = nextPosition - targetPosition;
        outward.y = 0f;

        float contactDistance = bodyRadius + playerRadius;
        if (outward.sqrMagnitude >= contactDistance * contactDistance) return;

        if (outward.sqrMagnitude < 0.000001f)
        {
            outward = framePosition - targetPosition;
            outward.y = 0f;

            if (outward.sqrMagnitude < 0.000001f) outward = Vector3.right;
        }

        nextPosition = targetPosition + outward.normalized * contactDistance;
        nextPosition.y = framePosition.y;
    }

    internal Vector3 GetOverlapDirection(EnemyMovement other)
    {
        int pair = enemyId ^ other.enemyId;
        float angle = (pair & 1023) * (Mathf.PI * 2f / 1024f);
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return enemyId < other.enemyId ? direction : -direction;
    }

    internal void Rotate(Vector3 direction, float stepDeltaTime)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * stepDeltaTime);
    }
}
