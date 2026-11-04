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
        // 적 자신의 접근만 멈춘다. 플레이어가 겹쳐 들어와도 위치를 바깥으로 밀어내지 않는다.
        float remainingDistance = Mathf.Max(0f, direction.magnitude - bodyRadius - playerRadius);
        if (remainingDistance <= 0.001f)
        {
            velocity = Vector3.zero;
            nextPosition = framePosition;
            return;
        }

        float chaseSpeed = Mathf.Min(moveSpeed, remainingDistance / stepDeltaTime);
        Vector3 desiredVelocity = direction.normalized * chaseSpeed + separation * (separationStrength * moveSpeed);
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, moveSpeed);
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, moveSpeed * 8f * stepDeltaTime);
        // 속도 보간에 남은 관성까지 제한한다. 현재 위치를 강제로 바깥으로 옮기지는 않는다.
        Vector3 movement = Vector3.ClampMagnitude(velocity * stepDeltaTime, remainingDistance);
        nextPosition = framePosition + movement;
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
