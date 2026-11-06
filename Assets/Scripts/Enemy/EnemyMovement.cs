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
    [SerializeField, Min(0)] private float damage = 10f;
    [SerializeField, Min(0.01f)] private float offscreenReturnDelay = 5f;

    [SerializeField, Min(0f)] private float knockbackDistance = 1f;
    [SerializeField, Min(0.01f)] private float knockbackDuration = 0.15f;

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
