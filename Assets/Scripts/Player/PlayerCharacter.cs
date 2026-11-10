using UnityEngine;

public class PlayerCharacter : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Renderer characterRenderer;
    [SerializeField] private Color antonioColor = new(0.35f, 0.7f, 0.4f);
    [SerializeField] private Color imeldaColor = new(0.65f, 0.4f, 0.9f);
    [SerializeField] private Color gennaroColor = new(0.4f, 0.65f, 0.95f);

    private int lastLevel = -1;

    private void Awake()
    {
        if (playerExperience == null) playerExperience = GetComponent<PlayerExperience>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
        if (characterRenderer == null) return;

        int id = DataManager.Instance.SelectedPlayerId;
        Color color = id == 2 ? imeldaColor : id == 3 ? gennaroColor : antonioColor;
        MaterialPropertyBlock block = new();
        characterRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        characterRenderer.SetPropertyBlock(block);
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
        PlayerData data = DataManager.Instance.SelectedPlayer;
        int steps = data.abilityLevelInterval > 0 ? playerExperience.Level / data.abilityLevelInterval : 1;
        float bonus = Mathf.Min(data.maximumAbilityBonus, steps * data.abilityBonusPerStep);
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
