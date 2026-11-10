using UnityEngine;

public class Spinach : Accessory
{
    private float damageBonusPerLevel => BonusPerLevel;
    private WeaponManager weaponManager;

    public override string DisplayName => "Spinach";
    public float DamageMultiplier => 1f + damageBonusPerLevel * Level;

    public override void Initialize(Transform owner)
    {
        weaponManager = owner.GetComponent<WeaponManager>();
    }

    public override string GetChoiceDescription()
    {
        return "무기 강화 후 피해량을 기준으로 모든 무기의 공격력이 "
            + (damageBonusPerLevel * 100f).ToString("0.#") + "% 증가합니다.";
    }

    protected override void ApplyEffect()
    {
        if (weaponManager != null) weaponManager.SetDamageMultiplier(DamageMultiplier);
    }
}
