using UnityEngine;
using Unity.Profiling;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class EnemyMovement : MonoBehaviour
{
    private static readonly ProfilerMarker physicsMoveMarker = new("EnemyMovement.PhysicsMove");
    private static readonly ProfilerMarker contactDamageMarker = new ProfilerMarker("EnemyMovement.ApplyContactDamage");

    [SerializeField] private Transform target;
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

    [SerializeField, Min(1)] private int dataId = 1;
    [SerializeField, Min(0.01f)] private float bodyRadius = 0.5f;
    [SerializeField, Min(0f)] private float playerRadius = 0.5f;

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
    private EnemyDefaultMovement defaultMovement;
    private EnemySwarmMovement swarmMovement;
    private Transform cachedDamageTarget;
    private IDamageable damageTarget;

    // 투사체 검색 격자와 이동 판단에 필요한 실행 상태
    internal EnemyMovement nextInCell;
    internal Vector3 framePosition;
    internal Vector3 crowdPosition;
    internal Vector3 crowdCorrection;
    internal int crowdContacts;
    [SerializeField, Min(0f)] private float swarmOverlapCorrectionSpeed = 3f;
    // 일반 적이 이벤트 박쥐에게 밀리는 보정 속도. 박쥐의 경로 보정과 분리한다.
    [SerializeField, Min(0f)] private float swarmPushSpeed = 3f;
    internal Vector3 PhysicsPosition => body.position;
    internal float CrowdRadius => isSwarm ? bodyRadius : collisionRadius;
    internal float CorrectionWeight => isSwarm ? swarmMovement.CorrectionWeight : 1f;
    internal Vector3 SwarmDirection => swarmMovement.Direction;
    internal float CorrectionSpeed => isSwarm
        ? swarmOverlapCorrectionSpeed * CorrectionWeight : swarmPushSpeed;

    internal void PrepareSwarmStep(float deltaTime)
    {
        framePosition = body.position;
        crowdPosition = isSwarm ? swarmMovement.Move(deltaTime) : framePosition;
    }

    internal void ApplyCrowdStep()
    {
        Vector3 position = isSwarm ? swarmMovement.ClampPosition(crowdPosition) : crowdPosition;
        crowdPosition = position;
        if ((position - body.position).sqrMagnitude > 0f)
        {
            body.position = position;
            recordPhysicsMovement = false;
        }
        if (isSwarm) body.rotation = Quaternion.LookRotation(swarmMovement.Direction, Vector3.up);
    }

    internal Vector3 targetPosition;

    internal int enemyId;
    internal float sidePreference;

    internal bool IsSwarm => isSwarm;
    internal int ActivationVersion { get; private set; }
    internal float BodyRadius => bodyRadius;
    // 무기 명중 반경은 기존 값을 유지하고 물리 이동의 접촉 거리는 Collider와 맞춘다.
    internal float MovementRadius => isSwarm ? bodyRadius : collisionRadius;
    internal float PlayerRadius => playerRadius;

    internal float MoveSpeed => moveSpeed;
    internal float ObstacleLookAhead => obstacleLookAhead;
    internal float AvoidanceCheckInterval => avoidanceCheckInterval;
    internal float ObstacleClearance => obstacleClearance;
    internal float OffscreenReturnDelay => offscreenReturnDelay;

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
        swarmMovement.Configure(start, direction, speed, pushStrength, maxOffset, recoverySpeed);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.collisionDetectionMode = CollisionDetectionMode.Discrete;
        body.isKinematic = true;
        bodyCollider.enabled = false;
        body.rotation = Quaternion.LookRotation(swarmMovement.Direction, Vector3.up);
        gameObject.layer = SwarmLayer;
        recordPhysicsMovement = false;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<SphereCollider>();
        if (body == null || bodyCollider == null)
        {
            Debug.LogError("EnemyMovement requires Rigidbody and SphereCollider.", this);
            enabled = false;
            return;
        }
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
        defaultMovement = new EnemyDefaultMovement(this);
        swarmMovement = new EnemySwarmMovement(this);
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
        swarmMovement.Reset();
        ActivationVersion++;
        cachedDamageTarget = null;
        damageTarget = null;
        enemyId = GetInstanceID();
        sidePreference = ((enemyId >> 2) & 1) == 0 ? 1f : -1f;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        defaultMovement.Reset();
        EnemyCrowd.Register(this);
        if (EnemyCrowd.IsFrozen) ClearFrozenMotion();
    }

    private void OnDisable()
    {
        if (body != null && !body.isKinematic)
            body.linearVelocity = body.angularVelocity = Vector3.zero;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = false;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        EnemyCrowd.Unregister(this);
        isSwarm = false;
        swarmMovement.Reset();
    }

    internal bool ShouldReturnOffscreen(bool inside, float now)
    {
        return isSwarm ? swarmMovement.ShouldReturnOffscreen(inside)
            : defaultMovement.ShouldReturnOffscreen(inside, now);
    }

    private void FixedUpdate()
    {
        using var scope = physicsMoveMarker.Auto();
        EnemyCrowd.UpdateSwarms();
        bool frozen = EnemyCrowd.IsFrozen;
        if (physicsFrozen != frozen)
        {
            physicsFrozen = frozen;
            body.constraints = frozen ? RigidbodyConstraints.FreezeAll : MovementConstraints;
            if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
            recordPhysicsMovement = false;
        }
        if (frozen || isSwarm) return;
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
        if (TryGetKnockbackMovement(dt, out Vector3 knockback))
        {
            // 한 스텝의 이동 구간을 검사하여 빠른 넉백의 장애물 관통을 방지한다.
            if (EnemyObstacleCollision.CastSphere(body.position, knockback, collisionRadius, out RaycastHit hit))
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
            body.linearVelocity = defaultMovement.GetVelocity(direction, body.linearVelocity, dt, chaseAcceleration);
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
        ApplyContactDamage();
    }

    private void ApplyContactDamage()
    {
        using var scope = contactDamageMarker.Auto();

        if (EnemyCrowd.IsFrozen || Time.deltaTime <= 0f || target == null) return;
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
        if (EnemyCrowd.IsFrozen) return;
        direction.y = 0f;
        if (strength <= 0f || knockbackDistance <= 0f || direction.sqrMagnitude <= 0.000001f) return;
        // 새 명중은 밀림을 무한히 누적하지 않고 방향과 지속 시간을 갱신한다.
        defaultMovement.ResetObstacleAvoidance();
        recordPhysicsMovement = false;
        knockbackRemaining = Mathf.Max(0.01f, knockbackDuration);
        knockbackVelocity = direction.normalized * (knockbackDistance * strength / knockbackRemaining);
    }

    internal void ClearFrozenMotion()
    {
        if (!body.isKinematic) body.linearVelocity = body.angularVelocity = Vector3.zero;
        body.constraints = RigidbodyConstraints.FreezeAll;
        physicsFrozen = true;
        recordPhysicsMovement = false;
        wasPhysicsKnockback = false;
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
        return EnemyObstacleCollision.CastSphere(framePosition, direction, probeRadius, out hit);
    }

}
