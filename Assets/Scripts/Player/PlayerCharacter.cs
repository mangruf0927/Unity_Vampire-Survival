using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCharacter : MonoBehaviour, IObserver
{
    [System.Serializable]
    private class CharacterPrefab
    {
        [Min(1)] public int playerId;
        public GameObject prefab;
    }

    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Transform visualRoot;
    [FormerlySerializedAs("characterVisuals")]
    [SerializeField] private CharacterPrefab[] characterPrefabs;

    private PlayerData data;
    private int lastLevel = -1;
    private PlayerMovement playerMovement;
    private Animator animator;
    private bool isDying;

    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int DieParameter = Animator.StringToHash("Die");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int DieState = Animator.StringToHash("Base Layer.Die");

    public bool IsDeathAnimationComplete
    {
        get
        {
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) return true;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.fullPathHash == DieState && state.normalizedTime >= 1f && !animator.IsInTransition(0);
        }
    }

    private void Awake()
    {
        if (playerExperience == null) playerExperience = GetComponent<PlayerExperience>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
        playerMovement = GetComponent<PlayerMovement>();
        data = DataManager.Instance.SelectedPlayer;
        CreateCharacter();
    }

    private void CreateCharacter()
    {
        if (visualRoot == null || characterPrefabs == null)
        {
            Debug.LogError("PlayerCharacter: Visual Root와 캐릭터 외형 목록을 설정해 주세요.", this);
            return;
        }

        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            CharacterPrefab visual = characterPrefabs[i];
            if (visual == null || visual.playerId != data.Id) continue;
            if (visual.prefab == null) break;

            GameObject instance = Instantiate(visual.prefab, visualRoot);
            instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            animator = instance.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            {
                animator.applyRootMotion = false;
                animator.ResetTrigger(DieParameter);
                animator.SetBool(IsMovingParameter, false);
                animator.Play(IdleState, 0, 0f);
            }
            return;
        }
        Debug.LogError($"PlayerCharacter: 캐릭터 ID {data.Id}의 외형 프리팹을 등록해 주세요.", this);
    }

    private void OnEnable()
    {
        lastLevel = -1;
        if (playerExperience != null) playerExperience.AddObserver(this);
        Notify();
    }

    private void OnDisable()
    {
        if (playerExperience != null) playerExperience.RemoveObserver(this);
    }

    private void LateUpdate()
    {
        if (isDying || Time.deltaTime <= 0f || animator == null || !animator.isActiveAndEnabled
            || animator.runtimeAnimatorController == null) return;
        animator.SetBool(IsMovingParameter, playerMovement != null && playerMovement.IsMoving);
    }

    public bool TryPlayDeathAnimation()
    {
        if (isDying) return true;
        if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null
            || !animator.HasState(0, DieState)) return false;

        isDying = true;
        animator.SetBool(IsMovingParameter, false);
        animator.ResetTrigger(DieParameter);
        // 게임이 정지해도 이 외형의 사망 애니메이션만 끝까지 재생한다.
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.SetTrigger(DieParameter);
        return true;
    }

    public void Notify()
    {
        if (weaponManager == null || playerExperience == null || lastLevel == playerExperience.Level) return;
        lastLevel = playerExperience.Level;
        ApplyAbility();
    }

    private void ApplyAbility()
    {
        int steps = data.abilityInterval > 0 ? playerExperience.Level / data.abilityInterval : 1;
        float bonus = Mathf.Min(data.maxAbilityBonus, steps * data.abilityBonus);
        float damage = 1f;
        float speed = 1f;
        int count = 0;
        switch (data.ability)
        {
            case CharacterAbility.DamagePerLevel: damage += bonus; break;
            case CharacterAbility.ProjectileSpeedPerLevel: speed += bonus; break;
            case CharacterAbility.ExtraProjectile: count = Mathf.RoundToInt(bonus); break;
        }
        weaponManager.SetCharacterBonuses(damage, speed, count);
    }
}
