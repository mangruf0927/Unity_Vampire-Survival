using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private LayerMask groundLayers = 1 << 9;

    private float maxHealth;
    private float gemDropChance;
    private float currentHealth;
    private bool isDead;

    public static event System.Action EnemyKilled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetKillNotifications() => EnemyKilled = null;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int HitParameter = Animator.StringToHash("Hit");
    private static readonly int DieParameter = Animator.StringToHash("Die");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int DieState = Animator.StringToHash("Base Layer.Die");
    private static readonly int DieStateName = Animator.StringToHash("Die");

    private EnemyMovement movement;
    private Animator animator;
    private AnimatorCullingMode animatorCullingMode;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        animator = GetComponentInChildren<Animator>(true);
        if (animator != null) animatorCullingMode = animator.cullingMode;

        EnemyData data = DataManager.Instance.Enemies.Get(movement.DataId);
        maxHealth = data.maxHealth;
        gemDropChance = data.gemDropChance;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
        ResetAnimation();
    }

    private void ResetAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return;

        animator.cullingMode = animatorCullingMode;
        animator.Rebind();
        animator.ResetTrigger(HitParameter);
        animator.ResetTrigger(DieParameter);
        animator.SetBool(IsMovingParameter, false);
        animator.Play(IdleState, 0, 0f);
        animator.Update(0f);
    }

    private void Update()
    {
        if (!isDead || Time.deltaTime <= 0f) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash == DieStateName && state.normalizedTime >= 1f && !animator.IsInTransition(0))
            ObjectPool.Instance.TryReturnToPool(gameObject);
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.EnemyHit);
        if (currentHealth > 0f)
        {
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                animator.ResetTrigger(HitParameter);
                animator.SetTrigger(HitParameter);
            }
            return;
        }

        isDead = true;
        movement.StopForDeath();
        EnemyKilled?.Invoke();
        DropExperienceGem();

        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null
            || !animator.HasState(0, DieState))
        {
            ObjectPool.Instance.TryReturnToPool(gameObject);
            return;
        }

        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.SetBool(IsMovingParameter, false);
        animator.ResetTrigger(HitParameter);
        animator.ResetTrigger(DieParameter);
        animator.SetTrigger(DieParameter);
    }

    private void DropExperienceGem()
    {
        if (gemDropChance <= 0f || (gemDropChance < 1f && Random.value >= gemDropChance)) return;
        Vector3 origin = transform.position + Vector3.up;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayers, QueryTriggerInteraction.Ignore)) return;

        GameObject gem = ObjectPool.Instance.GetFromPool(PoolTypeEnums.ExperienceGem, hit.point, Quaternion.identity);
        if (gem != null && gem.TryGetComponent(out ExperienceGem experienceGem))
            gem.transform.position = hit.point + Vector3.up * experienceGem.GroundOffset;
    }

}
