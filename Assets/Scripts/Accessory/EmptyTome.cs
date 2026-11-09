using UnityEngine;

public class EmptyTome : Accessory
{
    [SerializeField, Min(0f)] private float cooldownReductionPerLevel = 0.08f;
    private WeaponManager weaponManager;

    public override string DisplayName => "Empty Tome";
    public float CooldownMultiplier => Mathf.Clamp01(1f - cooldownReductionPerLevel * Level);

    public override void Initialize(Transform owner)
    {
        weaponManager = owner.GetComponent<WeaponManager>();
    }

    public override string GetChoiceDescription()
    {
        return "모든 무기의 쿨타임이 기본값의 "
            + (cooldownReductionPerLevel * 100f).ToString("0.#") + "%만큼 감소합니다.";
    }

    protected override void ApplyEffect()
    {
        if (weaponManager != null) weaponManager.SetCooldownMultiplier(CooldownMultiplier);
    }
}
