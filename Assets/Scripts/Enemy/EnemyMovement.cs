using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(1)] private int dataId = 1;
    public int DataId => dataId;
    private float moveSpeed = 3f;
    private float rotationSpeed = 360f;
    [SerializeField, Min(0.01f)] private float bodyRadius = 0.5f;
    [SerializeField, Min(0f)] private float playerRadius = 0.5f;
    [SerializeField, Min(0f)] private float separationRange = 1.5f;
    [SerializeField, Min(0f)] private float separationStrength = 2f;
    [SerializeField, Min(0f)] private float overlapCorrectionSpeed = 3f;
    private float damage = 10f;
    private float offscreenReturnDelay = 5f;

    [SerializeField, Min(0.1f)] private float obstacleLookAhead = 1.8f;
    [SerializeField, Min(0.01f)] private float avoidanceCheckInterval = 0.15f;
    [SerializeField, Min(0.01f)] private float obstacleClearance = 0.15f;

    private bool avoidingObstacle;
    private Vector3 avoidanceDirection;
    private bool canAvoidObstacles;
    private float avoidanceSide;
    private float avoidanceCheckRemaining;
    private float obstacleBlockedTime;

    private float knockbackDistance = 1f;
    private float knockbackDuration = 0.15f;

    private Vector3 knockbackVelocity;
    private float knockbackRemaining;
    private float offscreenSince = -1f;
    private bool isSwarm;
    private Vector3 swarmStart;
    private Vector3 swarmDirection;
    private bool swarmHasEnteredScreen;
    private float swarmProgress;
    private float swarmSpeed;
    private float swarmPushStrength;
    private float swarmMaxOffset;
    private float swarmRecoverySpeed;

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

    internal bool IsSwarm => isSwarm;
    internal Vector3 SwarmDirection => swarmDirection;
    internal float CorrectionWeight => isSwarm ? 1f / swarmPushStrength : 1f;
    internal int ActivationVersion { get; private set; }
    internal Transform Target => target;
    internal float BodyRadius => bodyRadius;
    internal float PlayerRadius => playerRadius;
    internal float SeparationRange => separationRange;
    internal float OverlapCorrectionSpeed => overlapCorrectionSpeed * CorrectionWeight;

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void ConfigureSwarm(Vector3 start, Vector3 direction, float speed,
        float pushStrength, float maxOffset, float recoverySpeed)
    {
        if (!isSwarm)
        {
            isSwarm = true;
            EnemyCrowd.RegisterSwarm();
        }
        swarmStart = start;
        direction.y = 0f;
        swarmDirection = direction.normalized;
        swarmHasEnteredScreen = false;
        swarmProgress = 0f;
        swarmSpeed = Mathf.Max(0.01f, speed);
        swarmPushStrength = Mathf.Max(1f, pushStrength);
        swarmMaxOffset = Mathf.Max(0f, maxOffset);
        swarmRecoverySpeed = Mathf.Max(0f, recoverySpeed);
        transform.rotation = Quaternion.LookRotation(swarmDirection, Vector3.up);
    }

    private void MoveSwarm(float stepDeltaTime)
    {
        Vector3 offset = framePosition - (swarmStart + swarmDirection * swarmProgress);
        offset.y = 0f;
        offset = Vector3.MoveTowards(offset, Vector3.zero, swarmRecoverySpeed * stepDeltaTime);
        if (knockbackRemaining > 0f)
        {
            float duration = Mathf.Min(stepDeltaTime, knockbackRemaining);
            offset += knockbackVelocity * duration;
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - duration);
        }
        swarmProgress += swarmSpeed * stepDeltaTime;
        nextPosition = swarmStart + swarmDirection * swarmProgress + Vector3.ClampMagnitude(offset, swarmMaxOffset);
        velocity = swarmDirection * swarmSpeed;
    }

    internal Vector3 ClampSwarmPosition(Vector3 position)
    {
        Vector3 ideal = swarmStart + swarmDirection * swarmProgress;
        Vector3 offset = position - ideal;
        offset.y = 0f;
        return ideal + Vector3.ClampMagnitude(offset, swarmMaxOffset);
    }

    private void Awake()
    {
        EnemyData data = DataManager.Instance.Enemies.Get(dataId);
        moveSpeed = data.moveSpeed;
        rotationSpeed = data.rotationSpeed;
        damage = data.damage;
        offscreenReturnDelay = data.offscreenReturnDelay;
        knockbackDistance = data.knockbackDistance;
        knockbackDuration = data.knockbackDuration;
    }

    private void OnEnable()
    {
        isSwarm = false;
        swarmHasEnteredScreen = false;
        swarmProgress = 0f;
        ActivationVersion++;
        offscreenSince = -1f;
        cachedDamageTarget = null;
        damageTarget = null;
        enemyId = GetInstanceID();
        sidePreference = ((enemyId >> 2) & 1) == 0 ? 1f : -1f;
        velocity = Vector3.zero;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        ResetObstacleAvoidance();
        avoidanceCheckRemaining = Mathf.Max(0.01f, avoidanceCheckInterval) * ((enemyId & 7) / 8f);
        EnemyCrowd.Register(this);
    }

    private void OnDisable()
    {
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        EnemyCrowd.Unregister(this);
        isSwarm = false;
        velocity = Vector3.zero;
    }

    internal bool ShouldReturnOffscreen(bool inside, float now)
    {
        if (isSwarm)
        {
            if (inside) swarmHasEnteredScreen = true;
            return swarmHasEnteredScreen && !inside;
        }

        if (inside)
        {
            offscreenSince = -1f;
            return false;
        }

        if (offscreenSince < 0f) offscreenSince = now;
        return now - offscreenSince >= offscreenReturnDelay;
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

    public void ApplyKnockback(Vector3 direction, float strength)
    {
        direction.y = 0f;
        if (strength <= 0f || knockbackDistance <= 0f || direction.sqrMagnitude <= 0.000001f) return;
        // 새 명중은 밀림을 무한히 누적하지 않고 방향과 지속 시간을 갱신한다.
        ResetObstacleAvoidance();
        knockbackRemaining = Mathf.Max(0.01f, knockbackDuration);
        knockbackVelocity = direction.normalized * (knockbackDistance * strength / knockbackRemaining);
        velocity = Vector3.zero;
    }

    internal Vector3 GetTargetDirection()
    {
        Vector3 direction = targetPosition - framePosition;
        direction.y = 0f;
        return direction;
    }

    internal void Move(Vector3 direction, Vector3 separation, float stepDeltaTime)
    {
        canAvoidObstacles = false;
        if (isSwarm)
        {
            MoveSwarm(stepDeltaTime);
            return;
        }
        if (knockbackRemaining > 0f)
        {
            float duration = Mathf.Min(stepDeltaTime, knockbackRemaining);
            nextPosition = framePosition + knockbackVelocity * duration;
            knockbackRemaining = Mathf.Max(0f, knockbackRemaining - duration);
            velocity = Vector3.zero;
            return;
        }

        float remainingDistance = Mathf.Max(0f, direction.magnitude - bodyRadius - playerRadius);
        if (remainingDistance <= 0.001f)
        {
            ResetObstacleAvoidance();
            velocity = Vector3.zero;
            nextPosition = framePosition;
            return;
        }

        canAvoidObstacles = true;
        UpdateObstacleAvoidance(direction, stepDeltaTime);
        float chaseSpeed = Mathf.Min(moveSpeed, remainingDistance / stepDeltaTime);
        Vector3 desiredVelocity = direction.normalized * chaseSpeed + separation * (separationStrength * moveSpeed);
        if (avoidingObstacle) desiredVelocity += avoidanceDirection * (moveSpeed * 1.5f);
        desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, moveSpeed);
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, moveSpeed * 8f * stepDeltaTime);

        Vector3 movement = Vector3.ClampMagnitude(velocity * stepDeltaTime, remainingDistance);
        nextPosition = framePosition + movement;
    }

    internal void RecordObstacleMovement(Vector3 requested, Vector3 actual, float deltaTime)
    {
        if (!canAvoidObstacles || isSwarm) return;
        // 군중 때문에 느려진 것이 아니라, 장애물 충돌로 실제 이동이 크게 줄었는지 확인한다.
        bool blocked = requested.sqrMagnitude > 0.00000001f
            && actual.sqrMagnitude < requested.sqrMagnitude * 0.25f;
        obstacleBlockedTime = blocked ? obstacleBlockedTime + deltaTime : 0f;
    }

    private void UpdateObstacleAvoidance(Vector3 targetDirection, float deltaTime)
    {
        avoidanceCheckRemaining -= deltaTime;
        if (!avoidingObstacle && obstacleBlockedTime < 0.15f) return;
        if (avoidanceCheckRemaining > 0f) return;
        avoidanceCheckRemaining = Mathf.Max(0.01f, avoidanceCheckInterval);

        float distance = Mathf.Max(0.1f, obstacleLookAhead);
        Vector3 probe = Vector3.ClampMagnitude(targetDirection, distance);
        if (!EnemyObstacleCollision.Cast(framePosition, probe, bodyRadius, out RaycastHit hit))
        {
            ResetObstacleAvoidance();
            return;
        }

        Vector3 normal = hit.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude <= 0.00001f) return;
        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        float side = avoidingObstacle ? avoidanceSide : sidePreference;
        Vector3 first = (tangent * (side * distance) + normal * obstacleClearance).normalized;
        Vector3 second = (tangent * (-side * distance) + normal * obstacleClearance).normalized;
        float firstSpace = GetFreeDistance(first, distance);
        float secondSpace = GetFreeDistance(second, distance);

        // 이동 중에는 방향을 유지하고, 선택한 쪽도 막혔을 때만 반대편을 시도한다.
        bool changeSide = !avoidingObstacle
            ? secondSpace > firstSpace + 0.05f
            : obstacleBlockedTime >= 0.5f && firstSpace < 0.05f && secondSpace > 0.1f;
        if (changeSide)
        {
            side = -side;
            first = second;
            firstSpace = secondSpace;
            obstacleBlockedTime = 0f;
        }
        avoidanceSide = side;
        avoidingObstacle = true;
        // 양쪽 모두 막혔다면 회피를 강제하지 않고 기존 추적과 분리에 맡긴다.
        avoidanceDirection = firstSpace > 0.02f ? first : Vector3.zero;
    }

    private float GetFreeDistance(Vector3 direction, float distance)
    {
        return EnemyObstacleCollision.Cast(framePosition, direction * distance, bodyRadius, out RaycastHit hit)
            ? hit.distance : distance;
    }

    private void ResetObstacleAvoidance()
    {
        avoidingObstacle = false;
        avoidanceDirection = Vector3.zero;
        obstacleBlockedTime = 0f;
        avoidanceCheckRemaining = 0f;
        canAvoidObstacles = false;
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
