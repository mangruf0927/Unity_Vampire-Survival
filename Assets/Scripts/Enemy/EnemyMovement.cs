using UnityEngine;

public abstract class EnemyMovement : MonoBehaviour
{
    [SerializeField, Min(1)] private int dataId = 1;
    [SerializeField] protected Transform target;
    [SerializeField, Min(0.01f)] private float bodyRadius = 0.5f;
    [SerializeField, Min(0f)] protected float playerRadius = 0.5f;

    protected Rigidbody body;
    private SphereCollider bodyCollider;
    private bool bodyColliderEnabled;
    private Animator movementAnimator;
    private Vector3 previousAnimationPosition;
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    protected float moveSpeed;
    protected float rotationSpeed;
    protected float offscreenReturnDelay;
    private float damage;
    private float knockbackDistance;
    private float knockbackDuration;

    private Vector3 knockbackVelocity;
    private float knockbackRemaining;
    private Transform cachedDamageTarget;
    private IDamageable damageTarget;

    internal EnemyMovement nextInCell;
    internal Vector3 framePosition;
    internal Vector3 targetPosition;
    internal int enemyId;
    internal float sidePreference;
    internal Vector3 crowdPosition;
    internal Vector3 crowdCorrection;
    internal int crowdContacts;

    public int DataId => dataId;
    internal bool IsDead { get; private set; }
    internal int ActivationVersion { get; private set; }
    internal float BodyRadius => bodyRadius;
    internal float PlayerRadius => playerRadius;
    internal float MoveSpeed => moveSpeed;
    internal float OffscreenReturnDelay => offscreenReturnDelay;
    internal Vector3 PhysicsPosition => body.position;
    internal Vector3 ColliderCenterOffset => bodyCollider != null
        ? body.rotation * Vector3.Scale(bodyCollider.center, transform.lossyScale) : Vector3.zero;
    internal float CrowdRadius => MovementRadius;
    internal abstract bool IsSwarm { get; }
    internal abstract float MovementRadius { get; }
    internal abstract float CorrectionSpeed { get; }
    internal virtual float CorrectionWeight => 1f;
    internal virtual Vector3 SwarmDirection => Vector3.zero;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<SphereCollider>();
        bodyColliderEnabled = bodyCollider != null && bodyCollider.enabled;
        movementAnimator = GetComponentInChildren<Animator>(true);
        if (body == null)
        {
            Debug.LogError("EnemyMovement requires Rigidbody.", this);
            enabled = false;
            return;
        }
        SetData();
    }

    private void SetData()
    {
        EnemyData data = DataManager.Instance.Enemies.Get(dataId);
        moveSpeed = data.moveSpeed;
        rotationSpeed = data.rotationSpeed;
        damage = data.damage;
        offscreenReturnDelay = data.offscreenReturnDelay;
        knockbackDistance = data.knockbackDistance;
        knockbackDuration = data.knockbackDuration;
    }

    protected virtual void OnEnable()
    {
        IsDead = false;
        if (bodyCollider != null) bodyCollider.enabled = bodyColliderEnabled;
        body.position = transform.position;
        body.rotation = transform.rotation;
        previousAnimationPosition = body.position;
        ActivationVersion++;
        cachedDamageTarget = null;
        damageTarget = null;
        enemyId = GetInstanceID();
        sidePreference = ((enemyId >> 2) & 1) == 0 ? 1f : -1f;
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        ResetMovement();
        EnemyCrowd.Register(this);
        if (EnemyCrowd.IsFrozen) ClearFrozenMotion();
    }

    protected virtual void OnDisable()
    {
        StopMovement();
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        EnemyCrowd.Unregister(this);
    }

    protected virtual void FixedUpdate()
    {
        EnemyCrowd.UpdateSwarms();
        UpdateMoveAnimation();
    }

    private void UpdateMoveAnimation()
    {
        Vector3 distance = body.position - previousAnimationPosition;
        distance.y = 0f;
        previousAnimationPosition = body.position;

        if (IsDead || movementAnimator == null || !movementAnimator.isActiveAndEnabled
            || movementAnimator.runtimeAnimatorController == null) return;
        movementAnimator.SetBool(IsMovingParameter, !EnemyCrowd.IsFrozen && distance.sqrMagnitude > 0.000001f);
    }

    protected virtual void LateUpdate()
    {
        ApplyContactDamage();
    }

    internal void StopForDeath()
    {
        if (IsDead) return;
        IsDead = true;
        StopMovement();
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        if (!body.isKinematic) body.constraints = RigidbodyConstraints.FreezeAll;
        if (bodyCollider != null) bodyCollider.enabled = false;
        EnemyCrowd.Unregister(this);
    }

    internal void ClearFrozenMotion()
    {
        knockbackRemaining = 0f;
        knockbackVelocity = Vector3.zero;
        OnFrozen();
    }

    internal virtual void PrepareSwarmStep(float deltaTime)
    {
        framePosition = body.position;
        crowdPosition = framePosition;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
        if (target != null && target.TryGetComponent(out CharacterController controller))
        {
            Vector3 scale = target.lossyScale;
            playerRadius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        }
    }

    private void ApplyContactDamage()
    {
        if (IsDead || EnemyCrowd.IsFrozen || Time.deltaTime <= 0f || target == null) return;
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
        if (IsDead || EnemyCrowd.IsFrozen) return;
        direction.y = 0f;
        if (strength <= 0f || knockbackDistance <= 0f || direction.sqrMagnitude <= 0.000001f) return;

        OnKnockbackApplied();
        knockbackRemaining = Mathf.Max(0.01f, knockbackDuration);
        knockbackVelocity = direction.normalized * (knockbackDistance * strength / knockbackRemaining);
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

    internal abstract void ApplyCrowdStep();
    internal abstract bool CheckScreenExit(bool inside, float now);
    protected abstract void ResetMovement();
    protected abstract void StopMovement();
    protected virtual void OnFrozen() { }
    protected virtual void OnKnockbackApplied() { }
}
