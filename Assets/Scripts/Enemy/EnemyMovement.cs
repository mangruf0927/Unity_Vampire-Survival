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
    [SerializeField, Min(0)] private int damage = 10;

    private Transform cachedDamageTarget;
    private IDamageable damageTarget;

    // EnemyCrowd와 공유하는 실행 중 상태
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
    internal float PlayerRadius => playerRadius;
    internal float SeparationRange => separationRange;
    internal float OverlapCorrectionSpeed => overlapCorrectionSpeed;

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    private void OnEnable()
    {
        cachedDamageTarget = null;
        damageTarget = null;
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
        ApplyContactDamage();
    }

    private void ApplyContactDamage()
    {
        if (Time.deltaTime <= 0f || target == null) return;
        if (cachedDamageTarget != target)
        {
            cachedDamageTarget = target;
            target.TryGetComponent(out damageTarget);
        }
        if (damageTarget == null) return;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        float contactDistance = bodyRadius + playerRadius + 0.002f;
        if (offset.sqrMagnitude <= contactDistance * contactDistance)
        {
            damageTarget.TakeDamage(damage);
        }
    }

    internal Vector3 GetTargetDirection()
    {
        Vector3 direction = targetPosition - framePosition;
        direction.y = 0f;
        return direction;
    }

    internal void Move(Vector3 direction, Vector3 separation, float stepDeltaTime)
    {
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

        Vector3 movement = Vector3.ClampMagnitude(velocity * stepDeltaTime, remainingDistance);
        nextPosition = framePosition + movement;
    }

    internal Vector3 GetOverlapDirection(EnemyMovement other)
    {
        int pair = enemyId ^ other.enemyId;
        float angle = (pair & 1023) * (Mathf.PI * 2f / 1024f);
        Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        return enemyId < other.enemyId ? direction : -direction;
    }

    internal void Rotate(Vector3 direction, float stepDeltaTime)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * stepDeltaTime);
    }
}
