using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Weapon[] weaponPrefabs;

#if UNITY_EDITOR
    [Tooltip("Editor 테스트용 시작 무기입니다. Weapon Prefabs에 등록된 프리팹을 연결하세요. None이면 캐릭터의 시작 무기를 사용합니다.")]
    [SerializeField] private Weapon testStartingWeapon;
#endif

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private TimeSystem timeSystem;

    [SerializeField, Min(0)] private int maximumWeapons = 3;
    private readonly Dictionary<Weapon, Weapon> ownedWeapons = new();
    private bool hasValidPrefabs;
    private float damageMultiplier = 1f;
    private float cooldownMultiplier = 1f;
    private float characterDamageMultiplier = 1f;
    private float characterSpeedMultiplier = 1f;
    private int characterProjectileBonus;

    public void SetCharacterBonuses(float damage, float speed, int count)
    {
        characterDamageMultiplier = damage;
        characterSpeedMultiplier = speed;
        characterProjectileBonus = count;
        foreach (Weapon weapon in ownedWeapons.Values)
            if (weapon != null) weapon.SetCharacterBonuses(damage, speed, count);
    }

    private readonly List<Weapon> acquisitionOrder = new();
    public IReadOnlyList<Weapon> OwnedWeapons => acquisitionOrder;
    public event System.Action OwnedItemsChanged;

    public int OwnedCount => ownedWeapons.Count;

    public void SetCooldownMultiplier(float multiplier)
    {
        cooldownMultiplier = Mathf.Max(0f, multiplier);
        foreach (Weapon weapon in ownedWeapons.Values)
            if (weapon != null) weapon.SetCooldownMultiplier(cooldownMultiplier);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0f, multiplier);
        foreach (Weapon weapon in ownedWeapons.Values)
            if (weapon != null) weapon.SetDamageMultiplier(damageMultiplier);
    }

    private void Awake()
    {
        hasValidPrefabs = ValidatePrefabs();
        if (!hasValidPrefabs) return;

        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (attackOrigin == null) attackOrigin = transform;
        if (gameCamera == null) gameCamera = Camera.main;
        if (timeSystem == null) timeSystem = FindFirstObjectByType<TimeSystem>();
#if UNITY_EDITOR
        if (testStartingWeapon != null)
        {
            if (AcquireOrUpgrade(testStartingWeapon)) return;
            Debug.LogWarning("테스트 시작 무기를 획득하지 못했습니다. Weapon Prefabs 등록과 최대 보유 개수를 확인해 주세요. 캐릭터의 시작 무기를 사용합니다.", this);
        }
#endif
        int startingId = DataManager.Instance.SelectedPlayer.weaponId;
        if (weaponPrefabs != null)
        {
            for (int i = 0; i < weaponPrefabs.Length; i++)
            {
                Weapon prefab = weaponPrefabs[i];
                if (prefab == null || prefab.DataId != startingId) continue;
                AcquireOrUpgrade(prefab);
                return;
            }
        }
        Debug.LogError($"시작 무기 ID {startingId}를 Weapon Prefabs에 등록해 주세요.", this);
    }

    private bool ValidatePrefabs()
    {
        if (weaponPrefabs == null)
        {
            Debug.LogError("WeaponManager: Weapon Prefabs 목록을 설정해 주세요.", this);
            return false;
        }

        bool valid = true;
        HashSet<int> registeredIds = new();
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            Weapon prefab = weaponPrefabs[i];
            if (prefab == null)
            {
                Debug.LogError($"WeaponManager: Weapon Prefabs[{i}]가 비어 있습니다.", this);
                valid = false;
                continue;
            }
            if (!registeredIds.Add(prefab.DataId))
            {
                Debug.LogError($"WeaponManager: 무기 Data ID {prefab.DataId}가 중복 등록되었습니다. 프리팹: {prefab.name}", this);
                valid = false;
            }
            if (!DataManager.Instance.Weapons.TryGet(prefab.DataId, out WeaponData data))
            {
                Debug.LogError($"WeaponManager: 프리팹 {prefab.name}의 Data ID {prefab.DataId}가 Weapons 테이블에 없습니다.", this);
                valid = false;
                continue;
            }

            bool matchesAttackType = (data.attackType == WeaponAttackType.Projectile && (prefab is MagicWandWeapon || prefab is KnifeWeapon))
                || (data.attackType == WeaponAttackType.Orbit && prefab is KingBibleWeapon)
                || (data.attackType == WeaponAttackType.Slash && prefab is WhipWeapon);
            if (!matchesAttackType)
            {
                Debug.LogError($"WeaponManager: 프리팹 {prefab.name}의 구현 {prefab.GetType().Name}과 Data ID {data.Id}의 공격 유형 {data.attackType}이 일치하지 않습니다.", this);
                valid = false;
            }
        }

        foreach (PlayerData player in DataManager.Instance.Players.All.Values)
        {
            if (registeredIds.Contains(player.weaponId)) continue;
            Debug.LogError($"WeaponManager: 캐릭터 ID {player.Id}의 시작 무기 ID {player.weaponId}가 Weapon Prefabs에 등록되지 않았습니다.", this);
            valid = false;
        }
        return valid;
    }

    private Vector3 GetAttackDirection()
    {
        return playerMovement != null ? playerMovement.LastMoveDirection : attackOrigin.forward;
    }

    private void Update()
    {
        if (!hasValidPrefabs) return;
        Vector3 direction = GetAttackDirection();
        foreach (Weapon weapon in ownedWeapons.Values)
            if (weapon != null) weapon.SetAttackDirection(direction);
    }

    public Weapon GetOwnedWeapon(Weapon prefab)
    {
        if (prefab == null) return null;
        ownedWeapons.TryGetValue(prefab, out Weapon weapon);
        return weapon;
    }

    public void AddChoices(List<ILevelUpOption> choices)
    {
        if (!hasValidPrefabs) return;
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            Weapon prefab = weaponPrefabs[i];
            if (prefab == null) continue;
            Weapon owned = GetOwnedWeapon(prefab);
            if (owned == null && OwnedCount >= maximumWeapons) continue;
            Weapon choice = owned != null ? owned : prefab;
            if (choice.CanUpgrade && !choices.Contains(choice)) choices.Add(choice);
        }
    }

    public bool AcquireOrUpgrade(Weapon choice)
    {
        if (!hasValidPrefabs || choice == null) return false;
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            Weapon prefab = weaponPrefabs[i];
            if (prefab == null) continue;
            Weapon owned = GetOwnedWeapon(prefab);
            if (choice != prefab && choice != owned) continue;

            if (owned != null)
            {
                if (!owned.CanUpgrade) return false;
                owned.LevelUp();
            }
            else
            {
                if (OwnedCount >= maximumWeapons) return false;
                Weapon instance = Instantiate(prefab, transform);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.Initialize(attackOrigin, gameCamera, GetAttackDirection());
                instance.SetDamageMultiplier(damageMultiplier);
                instance.SetCooldownMultiplier(cooldownMultiplier);
                instance.SetCharacterBonuses(characterDamageMultiplier, characterSpeedMultiplier, characterProjectileBonus);
                instance.Acquire();
                instance.AcquisitionTime = timeSystem != null ? timeSystem.ElapsedSeconds : 0f;
                ownedWeapons[prefab] = instance;
                acquisitionOrder.Add(instance);
                OwnedItemsChanged?.Invoke();
            }
            return true;
        }
        return false;
    }
}
