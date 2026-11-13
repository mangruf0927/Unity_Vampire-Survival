using UnityEngine;

// 물리 기반 비교용 이동. 적끼리의 별도 분리·겹침 보정은 구현하지 않는다.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class ColliderCrowdTestEnemy : MonoBehaviour
{

    private Rigidbody body;
    private SphereCollider bodyCollider;
    private EnemyCrowdComparison owner;
    private Transform target;
    private IDamageable damageTarget;
    private float speed, rotationSpeed, stopDistance, acceleration, damage;
    private float knockbackDistance, knockbackDuration, knockbackRemaining;
    private Vector3 knockbackVelocity;
    private bool frozen, swarm, avoiding, recordBlocked;
    private Vector3 lastPosition, requestedMovement, avoidanceDirection;
    private float blockedTime, probeTimer, avoidanceSide;
    private Vector3 swarmStart, swarmDirection;
    private float swarmProgress, swarmSpeed, swarmMaxOffset, swarmRecoverySpeed;
    private RigidbodyConstraints baseConstraints;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<SphereCollider>();
        baseConstraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    }

    public void Initialize(EnemyCrowdComparison owner, Transform target, EnemyData data,
        float radius, float depenetration, float acceleration)
    {
        this.owner = owner;
        this.target = target;
        target.TryGetComponent(out damageTarget);
        speed = data.moveSpeed;
        rotationSpeed = data.rotationSpeed;
        damage = data.damage;
        knockbackDistance = data.knockbackDistance;
        knockbackDuration = data.knockbackDuration;
        stopDistance = 1f;
        this.acceleration = Mathf.Max(0.01f, acceleration);
        bodyCollider.radius = Mathf.Clamp(radius, 0.1f, 0.5f);
        body.maxDepenetrationVelocity = Mathf.Max(0.01f, depenetration);
        body.mass = 1f;
        body.constraints = baseConstraints;
        body.linearVelocity = body.angularVelocity = Vector3.zero;
        knockbackRemaining = 0f;
        frozen = swarm = false;
        ResetAvoidance();
        probeTimer = (GetInstanceID() & 7) * 0.02f;
        SetFrozen(owner.IsFrozen);
    }

    public void ConfigureSwarm(Vector3 start, Vector3 direction, float speed, float mass, float maxOffset, float recoverySpeed)
    {
        swarm = true;
        swarmStart = start;
        swarmDirection = direction.normalized;
        swarmProgress = 0f;
        swarmSpeed = speed;
        swarmMaxOffset = maxOffset;
        swarmRecoverySpeed = recoverySpeed;
        body.mass = mass;
        body.rotation = Quaternion.LookRotation(swarmDirection, Vector3.up);
    }

    public void ApplyKnockback(Vector3 direction, float strength)
    {
        if (owner.IsFrozen || strength <= 0f) return;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.000001f) return;
        knockbackRemaining = Mathf.Max(0.01f, knockbackDuration);
        knockbackVelocity = direction.normalized * (knockbackDistance * strength / knockbackRemaining);
        ResetAvoidance();
    }

    public void SetFrozen(bool value)
    {
        if (frozen == value) return;
        frozen = value;
        body.linearVelocity = body.angularVelocity = Vector3.zero;
        body.constraints = value ? RigidbodyConstraints.FreezeAll : baseConstraints;
        knockbackRemaining = 0f;
        ResetAvoidance();
    }

    private void FixedUpdate()
    {
        if (owner == null || target == null) return;
        SetFrozen(owner.IsFrozen);
        if (frozen) return;
        float dt = Time.fixedDeltaTime;
        Vector3 direction = target.position - body.position;
        direction.y = 0f;
        if (swarm) MoveSwarm(dt);
        else if (knockbackRemaining > 0f)
        {
            body.linearVelocity = knockbackVelocity * (Mathf.Min(dt, knockbackRemaining) / dt);
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - dt);
            recordBlocked = false;
        }
        else MoveDefault(direction, dt);

        Vector3 facing = swarm ? swarmDirection : direction;
        if (facing.sqrMagnitude > 0.000001f)
            body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                Quaternion.LookRotation(facing, Vector3.up), rotationSpeed * dt));
        if (direction.sqrMagnitude <= (stopDistance + 0.002f) * (stopDistance + 0.002f))
            damageTarget?.TakeDamage(damage);
    }

    private void MoveDefault(Vector3 direction, float dt)
    {
        float remaining = Mathf.Max(0f, direction.magnitude - stopDistance);
        if (remaining <= 0.001f)
        {
            body.linearVelocity = Vector3.zero;
            ResetAvoidance();
            return;
        }
        if (recordBlocked)
        {
            Vector3 actual = body.position - lastPosition;
            blockedTime = requestedMovement.sqrMagnitude > 0.00000001f
                && actual.sqrMagnitude < requestedMovement.sqrMagnitude * 0.25f ? blockedTime + dt : 0f;
        }
        UpdateAvoidance(direction, dt);
        Vector3 desired = direction.normalized * Mathf.Min(speed, remaining / dt);
        if (avoiding) desired += avoidanceDirection * (speed * 1.5f);
        desired = Vector3.ClampMagnitude(desired, speed);
        body.linearVelocity = Vector3.ClampMagnitude(Vector3.MoveTowards(body.linearVelocity, desired, acceleration * dt), remaining / dt);
        lastPosition = body.position;
        requestedMovement = body.linearVelocity * dt;
        recordBlocked = true;
    }

    private void UpdateAvoidance(Vector3 direction, float dt)
    {
        probeTimer -= dt;
        if ((!avoiding && blockedTime < 0.15f) || probeTimer > 0f) return;
        probeTimer = 0.15f;
        if (!LegacyGridEnemyObstacleCollision.Cast(body.position, Vector3.ClampMagnitude(direction, 1.8f), bodyCollider.radius, out RaycastHit hit))
        {
            avoiding = false;
            avoidanceDirection = Vector3.zero;
            return;
        }
        Vector3 normal = hit.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude <= 0.00001f) return;
        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        float side = avoiding ? avoidanceSide : ((GetInstanceID() & 1) == 0 ? 1f : -1f);
        Vector3 first = (tangent * side + normal * 0.15f).normalized;
        Vector3 second = (-tangent * side + normal * 0.15f).normalized;
        float a = FreeDistance(first), b = FreeDistance(second);
        if ((!avoiding && b > a + 0.05f) || (avoiding && blockedTime >= 0.5f && a < 0.05f && b > 0.1f))
        {
            first = second;
            a = b;
            side = -side;
            blockedTime = 0f;
        }
        avoidanceSide = side;
        avoiding = true;
        avoidanceDirection = a > 0.02f ? first : Vector3.zero;
    }

    private float FreeDistance(Vector3 direction)
    {
        return LegacyGridEnemyObstacleCollision.Cast(body.position, direction * 1.8f, bodyCollider.radius, out RaycastHit hit) ? hit.distance : 1.8f;
    }

    private void ResetAvoidance()
    {
        avoiding = recordBlocked = false;
        blockedTime = probeTimer = 0f;
        avoidanceDirection = Vector3.zero;
    }

    private void MoveSwarm(float dt)
    {
        Vector3 offset = body.position - (swarmStart + swarmDirection * swarmProgress);
        offset.y = 0f;
        offset = Vector3.MoveTowards(offset, Vector3.zero, swarmRecoverySpeed * dt);
        if (knockbackRemaining > 0f)
        {
            offset += knockbackVelocity * Mathf.Min(dt, knockbackRemaining);
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - dt);
        }
        swarmProgress += swarmSpeed * dt;
        Vector3 desired = swarmStart + swarmDirection * swarmProgress + Vector3.ClampMagnitude(offset, swarmMaxOffset);
        // 순간이동하지 않고 물리 속도로 원래 경로에 복귀한다. 접촉 중 경로 오차도 비교 대상이다.
        body.linearVelocity = Vector3.ClampMagnitude((desired - body.position) / dt, swarmSpeed + swarmRecoverySpeed);
    }
}
