using UnityEngine;

public class EmptyTome : Accessory
{
    private WeaponManager weaponManager;

    public float CooldownMultiplier => Mathf.Clamp01(1f - BonusPerLevel * Level);

    public override void Initialize(Transform owner)
    {
        weaponManager = owner.GetComponent<WeaponManager>();
    }

    protected override void ApplyEffect()
    {
        if (weaponManager == null) return;
        weaponManager.SetCooldownMultiplier(CooldownMultiplier);
    }
}
