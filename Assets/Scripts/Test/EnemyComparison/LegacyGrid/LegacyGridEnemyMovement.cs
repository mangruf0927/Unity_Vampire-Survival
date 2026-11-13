// 비교 테스트 전용: Collider 전환 이전 격자 이동을 보관한다. 실제 게임에서는 사용하지 않는다.
using UnityEngine;
using Unity.Profiling;

public class LegacyGridEnemyMovement : MonoBehaviour
{
    private static readonly ProfilerMarker physicsMoveMarker = new("LegacyGridEnemyMovement.PhysicsMove");
    private static readonly ProfilerMarker contactDamageMarker = new ProfilerMarker("LegacyGridEnemyMovement.ApplyContactDamage");

    [SerializeField] private Transform target;
    // 비교 씬의 기존 격자 모드는 유지하고 실제 적 프리팹에서만 활성화한다.
    [SerializeField] private bool usePhysicsMovement;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.45f;
    [SerializeField, Min(0.01f)] private float chaseAcceleration = 24f;
    [SerializeField, Min(0.01f)] private float maxDepenetrationVelocity = 2f;
    private Rigidbody body;
    private SphereCollider bodyCollider;
    private bool physicsFrozen;
    private bool recordPhysicsMovement;
    private Vector3 previousPhysicsPosition;
    private Vector3 requestedPhysicsMovement;
    private bool wasPhysicsKnockback;
    private int regularLayer;
    private float regularMass;
    private CollisionDetectionMode regularCollisionMode;
    private const int SwarmLayer = 13;
    private const RigidbodyConstraints MovementConstraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
    internal bool UsesPhysics => usePhysicsMovement;

    [SerializeField, Min(1)] private int dataId = 1;
    [SerializeField, Min(0.01f)] private float bodyRadius = 0.5f;
    [SerializeField, Min(0f)] private float playerRadius = 0.5f;
    [SerializeField, Min(0f)] private float separationRange = 1.5f;
    [SerializeField, Min(0f)] private float separationStrength = 2f;
    [SerializeField, Min(0f)] private float overlapCorrectionSpeed = 3f;

    [SerializeField, Min(0.1f)] private float obstacleLookAhead = 1.8f;
    [SerializeField, Min(0.01f)] private float avoidanceCheckInterval = 0.15f;
    [SerializeField, Min(0.01f)] private float obstacleClearance = 0.15f;

    public int DataId => dataId;
    private float moveSpeed = 3f;
    private float rotationSpeed = 360f;
    private float damage = 10f;
    private float offscreenReturnDelay = 5f;

    private float knockbackDistance = 1f;
    private float knockbackDuration = 0.15f;

    private Vector3 knockbackVelocity;
    private float knockbackRemaining;
    private bool isSwarm;
    private LegacyGridEnemyDefaultMovement defaultMovement;
    private LegacyGridEnemySwarmMovement swarmMovement;
    private Transform cachedDamageTarget;
    private IDamageable damageTarget;

    // EnemyCrowd와 공유하는 실행 중 상태
    internal LegacyGridEnemyMovement nextInCell;
    internal Vector3 framePosition;
    internal Vector3 nextPosition;
    internal Vector3 targetPosition;
    internal Vector3 velocity;
    internal Vector3 correction;

    internal int contactCount;
    internal int enemyId;
    internal float sidePreference;

    internal bool IsSwarm => isSwarm;
    internal Vector3 SwarmDirection => swarmMovement.Direction;
    internal float CorrectionWeight => isSwarm ? swarmMovement.CorrectionWeight : 1f;
    internal int ActivationVersion { get; private set; }
    internal Transform Target => target;
    internal float BodyRadius => bodyRadius;
    // 무기 명중 반경은 기존 값을 유지하고 물리 이동의 접촉 거리는 Collider와 맞춘다.
    internal float MovementRadius => UsesPhysics ? collisionRadius : bodyRadius;
    internal float PlayerRadius => playerRadius;
    internal float SeparationRange => separationRange;
    internal float OverlapCorrectionSpeed => overlapCorrectionSpeed * CorrectionWeight;

    internal float MoveSpeed => moveSpeed;
    internal float SeparationStrength => separationStrength;
    internal float ObstacleLookAhead => obstacleLookAhead;
    internal float AvoidanceCheckInterval => avoidanceCheckInterval;
    internal float ObstacleClearance => obstacleClearance;
    internal float OffscreenReturnDelay => offscreenReturnDelay;

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void ConfigureSwarm(Vector3 start, Vector3 direction, float speed,
        float pushStrength, float maxOffset, float recoverySpeed, float acceleration = 6f)
    {
        if (!isSwarm)
        {
            isSwarm = true;
            LegacyGridEnemyCrowd.RegisterSwarm();
        }
        swarmMovement.Configure(start, direction, speed, pushStrength, maxOffset, recoverySpeed, acceleration);
        if (usePhysicsMovement)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.mass = Mathf.Max(1f, pushStrength);
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.rotation = Quaternion.LookRotation(swarmMovement.Direction, Vector3.up);
            gameObject.layer = SwarmLayer;
            recordPhysicsMovement = false;
        }
    }

    internal Vector3 ClampSwarmPosition(Vector3 position)
    {
        return swarmMovement.ClampSwarmPosition(position);
    }

    private void Awake()
    {
        if (usePhysicsMovement)
        {
            body = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<SphereCollider>();
            if (body == null || bodyCollider == null)
            {
                Debug.LogError("Physics movement requires Rigidbody and SphereCollider. Using legacy movement.", this);
                usePhysicsMovement = false;
            }
        }
        if (usePhysicsMovement)
        {
            regularLayer = gameObject.layer;
            regularMass = body.mass;
            regularCollisionMode = body.collisionDetectionMode;
            // SphereCollider는 가장 큰 축의 스케일로 확대되므로 월드 반경을 보정한다.
            Vector3 scale = transform.lossyScale;
            float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            bodyCollider.radius = collisionRadius / Mathf.Max(0.0001f, largestScale);
            body.useGravity = false;
            body.constraints = MovementConstraints;
            body.maxDepenetrationVelocity = maxDepenetrationVelocity;
        }
        defaultMovement = new LegacyGridEnemyDefaultMovement(this);
        swarmMovement = new LegacyGridEnemySwarmMovement(this);
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
        if (usePhysicsMovement)
        {
            body.isKinematic = false;
            body.position = transform.position;
            body.rotation = transform.rotation;
            bodyCollider.enabled = true;
            body.mass = regularMass;
            body.collisionDetectionMode = regularCollisionMode;
            gameObject.layer = regularLayer;
            physicsFrozen = false;
            body.constraints = MovementConstraints;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            recordPhysicsMovement = false;
            wasPhysicsKnockback = false;
        }
        swarmMovement.Reset();
        ActivationVersion++;
        cachedDamageTarget = null;
        damageTarget = null;
        enemyId = GetInstanceID();
        sidePreference = ((enemyId >> 2) & 1) == 0 ? 1f : -1f;
        velocity = Vector3.zero;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        defaultMovement.Reset();
        LegacyGridEnemyCrowd.Register(this);
        if (UsesPhysics && LegacyGridEnemyCrowd.IsFrozen) ClearFrozenMotion();
    }

    private void OnDisable()
    {
        if (usePhysicsMovement && !body.isKinematic)
            body.linearVelocity = body.angularVelocity = Vector3.zero;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = false;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        LegacyGridEnemyCrowd.Unregister(this);
        isSwarm = false;
        velocity = Vector3.zero;
    }

    internal bool ShouldReturnOffscreen(bool inside, float now)
    {
        return isSwarm ? swarmMovement.ShouldReturnOffscreen(inside)
            : defaultMovement.ShouldReturnOffscreen(inside, now);
    }

    private void FixedUpdate()
    {
        if (!UsesPhysics) return;
        using var scope = physicsMoveMarker.Auto();
        bool frozen = LegacyGridEnemyCrowd.IsFrozen;
        if (physicsFrozen != frozen)
        {
            physicsFrozen = frozen;
            body.constraints = frozen ? RigidbodyConstraints.FreezeAll : MovementConstraints;
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            recordPhysicsMovement = false;
        }
        if (frozen) return;
        if (target == null && !isSwarm)
        {
            body.linearVelocity = Vector3.zero;
            recordPhysicsMovement = false;
            return;
        }

        float dt = Time.fixedDeltaTime;
        framePosition = body.position;
        if (target != null) targetPosition = target.position;
        if (recordPhysicsMovement)
            defaultMovement.RecordObstacleMovement(requestedPhysicsMovement, body.position - previousPhysicsPosition, dt);
        Vector3 direction = GetTargetDirection();
        if (isSwarm)
        {
            body.linearVelocity = swarmMovement.GetPhysicsVelocity(body.linearVelocity, dt);
            recordPhysicsMovement = false;
        }
        else if (TryGetKnockbackMovement(dt, out Vector3 knockback))
        {
            // 한 스텝의 이동 구간을 검사하여 빠른 넉백의 장애물 관통을 방지한다.
            if (LegacyGridEnemyObstacleCollision.CastSphere(body.position, knockback, collisionRadius, out RaycastHit hit))
                knockback = Vector3.ClampMagnitude(knockback, Mathf.Max(0f, hit.distance - 0.002f));
            body.linearVelocity = knockback / dt;
            recordPhysicsMovement = false;
            wasPhysicsKnockback = true;
        }
        else
        {
            // 넉백 속도를 추적 가속도에 넘기면 종료 후에도 추가로 밀려난다.
            if (wasPhysicsKnockback) body.linearVelocity = Vector3.zero;
            wasPhysicsKnockback = false;
            body.linearVelocity = defaultMovement.GetPhysicsVelocity(direction, body.linearVelocity, dt, chaseAcceleration);
            previousPhysicsPosition = body.position;
            requestedPhysicsMovement = body.linearVelocity * dt;
            recordPhysicsMovement = true;
        }
        Vector3 facing = isSwarm ? swarmMovement.Direction : direction;
        if (facing.sqrMagnitude > 0.000001f)
            body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                Quaternion.LookRotation(facing, Vector3.up), rotationSpeed * dt));
    }

    private void LateUpdate()
    {
        LegacyGridEnemyCrowd.UpdateCrowd();
        ApplyContactDamage();
    }

    private void ApplyContactDamage()
    {
        using var scope = contactDamageMarker.Auto();

        if (LegacyGridEnemyCrowd.IsFrozen || Time.deltaTime <= 0f || target == null) return;
        if (cachedDamageTarget != target)
        {
            cachedDamageTarget = target;
            target.TryGetComponent(out damageTarget);
        }
        if (damageTarget == null) return;

        Vector3 offset = target.position - transform.position;
        offset.y = 0f;
        float contactDistance = MovementRadius + playerRadius + 0.002f;
        if (offset.sqrMagnitude <= contactDistance * contactDistance)
        {
            damageTarget.TakeDamage(damage);
        }
    }

    public void ApplyKnockback(Vector3 direction, float strength)
    {
        if (LegacyGridEnemyCrowd.IsFrozen) return;
        direction.y = 0f;
        if (strength <= 0f || knockbackDistance <= 0f || direction.sqrMagnitude <= 0.000001f) return;
        // 새 명중은 밀림을 무한히 누적하지 않고 방향과 지속 시간을 갱신한다.
        defaultMovement.ResetObstacleAvoidance();
        recordPhysicsMovement = false;
        knockbackRemaining = Mathf.Max(0.01f, knockbackDuration);
        knockbackVelocity = direction.normalized * (knockbackDistance * strength / knockbackRemaining);
        velocity = Vector3.zero;
    }

    internal void ClearFrozenMotion()
    {
        if (UsesPhysics)
        {
            body.linearVelocity = body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
            physicsFrozen = true;
            recordPhysicsMovement = false;
            wasPhysicsKnockback = false;
        }
        velocity = Vector3.zero;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        defaultMovement.ResetObstacleAvoidance();
    }

    internal bool TryGetKnockbackMovement(float deltaTime, out Vector3 movement)
    {
        movement = Vector3.zero;
        if (knockbackRemaining <= 0f) return false;
        float duration = Mathf.Min(deltaTime, knockbackRemaining);
        movement = knockbackVelocity * duration;
        knockbackRemaining = Mathf.Max(0f, knockbackRemaining - duration);
        return true;
    }

    internal Vector3 GetTargetDirection()
    {
        Vector3 direction = targetPosition - framePosition;
        direction.y = 0f;
        return direction;
    }

    internal bool CastObstacle(Vector3 direction, out RaycastHit hit)
    {
        // 접촉 중인 Collider와 같은 크기로 탐색하면 시작점이 접촉 영역에 걸려
        // 좌우 탐색도 거리 0으로 판정될 수 있다. 감지용 구체에만 작은 여유를 준다.
        float probeRadius = Mathf.Max(0.01f, collisionRadius - 0.02f);
        return UsesPhysics
            ? LegacyGridEnemyObstacleCollision.CastSphere(framePosition, direction, probeRadius, out hit)
            : LegacyGridEnemyObstacleCollision.Cast(framePosition, direction, bodyRadius, out hit);
    }

    internal void Move(Vector3 direction, float stepDeltaTime)
    {
        if (isSwarm) swarmMovement.Move(stepDeltaTime);
        else defaultMovement.Move(direction, stepDeltaTime);
    }

    internal void RecordObstacleMovement(Vector3 requested, Vector3 actual, float deltaTime)
    {
        if (!isSwarm) defaultMovement.RecordObstacleMovement(requested, actual, deltaTime);
    }

    internal Vector3 GetOverlapDirection(LegacyGridEnemyMovement other)
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
