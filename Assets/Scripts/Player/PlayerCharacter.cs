using UnityEngine;

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
    [SerializeField] private CharacterPrefab[] characterPrefabs;

    private PlayerData data;
    private int lastLevel = -1;

    private void Awake()
    {
        if (playerExperience == null) playerExperience = GetComponent<PlayerExperience>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
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
