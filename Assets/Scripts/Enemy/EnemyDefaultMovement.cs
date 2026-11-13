using UnityEngine;

public class EnemyDefaultMovement : EnemyMovement
{
    [SerializeField, Min(0.01f)] private float chaseAcceleration = 24f;
    [SerializeField, Min(0.1f)] private float obstacleLookAhead = 1.8f;
    [SerializeField, Min(0.01f)] private float avoidanceCheckInterval = 0.15f;
    [SerializeField, Min(0.01f)] private float obstacleClearance = 0.15f;
    [SerializeField, Min(0f)] private float swarmPushSpeed = 3f;

    private float collisionRadius;

    private RigidbodyConstraints regularConstraints;
    private bool physicsFrozen;
    private bool recordPhysicsMovement;
    private Vector3 previousPhysicsPosition;
    private Vector3 requestedPhysicsMovement;
    private bool wasPhysicsKnockback;

    private float offScreenTime = -1f;
    private bool isAvoiding;
    private bool canAvoid;
    private Vector3 avoidDirection;
    private float avoidSide;
    private float avoidCheckTimer;
    private float blockedTime;

    internal override bool IsSwarm => false;
    internal override float MovementRadius => collisionRadius;
    internal override float CorrectionSpeed => swarmPushSpeed;
    internal float AvoidanceCheckInterval => avoidanceCheckInterval;
    internal float ObstacleLookAhead => obstacleLookAhead;
    internal float ObstacleClearance => obstacleClearance;

    protected override void Awake()
    {
        base.Awake();
        SphereCollider collider = GetComponent<SphereCollider>();
        if (body == null || collider == null)
        {
            Debug.LogError("EnemyDefaultMovement requires Rigidbody and SphereCollider.", this);
            enabled = false;
            return;
        }
        regularConstraints = body.constraints;
        Vector3 scale = transform.lossyScale;
        float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        collisionRadius = collider.radius * largestScale;
    }

    protected override void ResetMovement()
    {
        physicsFrozen = false;
        body.constraints = regularConstraints;
        body.linearVelocity = body.angularVelocity = Vector3.zero;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = false;
        offScreenTime = -1f;
        ResetObstacleAvoidance();
        avoidCheckTimer = Mathf.Max(0.01f, AvoidanceCheckInterval) * ((enemyId & 7) / 8f);
    }

    protected override void StopMovement()
    {
        if (body != null && !body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = false;
    }

    protected override void OnFrozen()
    {
        if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
        body.constraints = RigidbodyConstraints.FreezeAll;
        physicsFrozen = true;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = false;
        ResetObstacleAvoidance();
    }

    protected override void OnKnockbackApplied()
    {
        ResetObstacleAvoidance();
        recordPhysicsMovement = false;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (UpdateFrozenState()) return;
        MoveRegularEnemy();
    }

    internal override void ApplyCrowdStep()
    {
        if ((crowdPosition - body.position).sqrMagnitude > 0f)
        {
            body.position = crowdPosition;
            recordPhysicsMovement = false;
        }
    }

    private bool UpdateFrozenState()
    {
        bool frozen = EnemyCrowd.IsFrozen;
        if (physicsFrozen != frozen)
        {
            physicsFrozen = frozen;
            body.constraints = frozen ? RigidbodyConstraints.FreezeAll : regularConstraints;
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            recordPhysicsMovement = false;
        }
        return frozen;
    }

    private void MoveRegularEnemy()
    {
        if (target == null)
        {
            body.linearVelocity = Vector3.zero;
            recordPhysicsMovement = false;
            return;
        }

        float dt = Time.fixedDeltaTime;
        framePosition = body.position;
        if (target != null) targetPosition = target.position;
        if (recordPhysicsMovement)
            CheckBlocked(requestedPhysicsMovement, body.position - previousPhysicsPosition, dt);
        Vector3 direction = GetTargetDirection();
        if (TryGetKnockbackMovement(dt, out Vector3 knockback))
            MoveKnockback(knockback, dt);
        else
            MoveTowardsTarget(direction, dt);
        RotateTowardsTarget(direction, dt);
    }

    private void MoveTowardsTarget(Vector3 direction, float dt)
    {
        if (wasPhysicsKnockback) body.linearVelocity = Vector3.zero;
        wasPhysicsKnockback = false;
        body.linearVelocity = GetVelocity(direction, body.linearVelocity, dt, chaseAcceleration);
        previousPhysicsPosition = body.position;
        requestedPhysicsMovement = body.linearVelocity * dt;
        recordPhysicsMovement = true;
    }

    private void MoveKnockback(Vector3 knockback, float dt)
    {
        if (EnemyObstacleCollision.CastSphere(body.position, knockback, collisionRadius, out RaycastHit hit))
            knockback = Vector3.ClampMagnitude(knockback, Mathf.Max(0f, hit.distance - 0.002f));
        body.linearVelocity = knockback / dt;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = true;
    }

    private void RotateTowardsTarget(Vector3 direction, float dt)
    {
        Vector3 facing = direction;
        if (facing.sqrMagnitude > 0.000001f)
            body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                Quaternion.LookRotation(facing, Vector3.up), rotationSpeed * dt));
    }

    internal Vector3 GetTargetDirection()
    {
        Vector3 direction = targetPosition - framePosition;
        direction.y = 0f;
        return direction;
    }

    internal bool CastObstacle(Vector3 direction, out RaycastHit hit)
    {
        float probeRadius = Mathf.Max(0.01f, collisionRadius - 0.02f);
        return EnemyObstacleCollision.CastSphere(framePosition, direction, probeRadius, out hit);
    }

    internal override bool CheckScreenExit(bool inside, float now)
    {
        if (inside)
        {
            offScreenTime = -1f;
            return false;
        }

        if (offScreenTime < 0f) offScreenTime = now;

        return now - offScreenTime >= OffscreenReturnDelay;
    }

    internal Vector3 GetVelocity(Vector3 direction, Vector3 currentVelocity, float deltaTime, float acceleration)
    {
        float remainingDistance = Mathf.Max(0f, direction.magnitude - MovementRadius - PlayerRadius);
        if (remainingDistance <= 0.001f)
        {
            ResetObstacleAvoidance();
            return Vector3.zero;
        }

        canAvoid = true;
        UpdateAvoidance(direction, deltaTime);

        Vector3 desired = direction.normalized * Mathf.Min(MoveSpeed, remainingDistance / deltaTime);
        if (isAvoiding) desired += avoidDirection * (MoveSpeed * 1.5f);
        desired = Vector3.ClampMagnitude(desired, MoveSpeed);

        currentVelocity.y = 0f;
        return Vector3.ClampMagnitude(Vector3.MoveTowards(currentVelocity, desired, acceleration * deltaTime), remainingDistance / deltaTime);
    }

    internal void CheckBlocked(Vector3 requested, Vector3 actual, float deltaTime)
    {
        if (!canAvoid) return;

        bool blocked = requested.sqrMagnitude > 0.00000001f && Vector3.Dot(actual, requested) < requested.sqrMagnitude * 0.5f;
        blockedTime = blocked ? blockedTime + deltaTime : 0f;
    }

    private void UpdateAvoidance(Vector3 targetDirection, float deltaTime)
    {
        avoidCheckTimer -= deltaTime;

        if (!isAvoiding && blockedTime < 0.15f) return;
        if (avoidCheckTimer > 0f) return;

        avoidCheckTimer = Mathf.Max(0.01f, AvoidanceCheckInterval);

        float distance = Mathf.Max(0.1f, ObstacleLookAhead);
        Vector3 probe = Vector3.ClampMagnitude(targetDirection, distance);

        if (!CastObstacle(probe, out RaycastHit hit))
        {
            ResetObstacleAvoidance();
            return;
        }

        ChooseAvoidDirection(hit.normal, distance);
    }

    private void ChooseAvoidDirection(Vector3 normal, float distance)
    {
        normal.y = 0f;
        if (normal.sqrMagnitude <= 0.00001f) return;

        normal.Normalize();

        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        float side = isAvoiding ? avoidSide : sidePreference;

        Vector3 first = (tangent * (side * distance) + normal * ObstacleClearance).normalized;
        Vector3 second = (tangent * (-side * distance) + normal * ObstacleClearance).normalized;

        float firstSpace = GetFreeDistance(first, distance);
        float secondSpace = GetFreeDistance(second, distance);

        ApplyAvoidDirection(side, first, second, firstSpace, secondSpace);
    }

    private void ApplyAvoidDirection(float side, Vector3 first, Vector3 second, float firstSpace, float secondSpace)
    {
        bool changeSide = !isAvoiding ? secondSpace > firstSpace + 0.05f : blockedTime >= 0.5f && firstSpace < 0.05f && secondSpace > 0.1f;

        if (changeSide)
        {
            side = -side;
            first = second;
            firstSpace = secondSpace;
            blockedTime = 0f;
        }

        avoidSide = side;
        isAvoiding = true;
        avoidDirection = firstSpace > 0.02f ? first : Vector3.zero;
    }

    private float GetFreeDistance(Vector3 direction, float distance)
    {
        return CastObstacle(direction * distance, out RaycastHit hit) ? hit.distance : distance;
    }

    internal void ResetObstacleAvoidance()
    {
        isAvoiding = false;
        avoidDirection = Vector3.zero;
        blockedTime = 0f;
        avoidCheckTimer = 0f;
        canAvoid = false;
    }
}
