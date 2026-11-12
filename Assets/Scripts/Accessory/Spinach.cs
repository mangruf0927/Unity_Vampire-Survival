using UnityEngine;

public class Spinach : Accessory
{
    private WeaponManager weaponManager;

    public float DamageMultiplier => 1f + BonusPerLevel * Level;

    public override void Initialize(Transform owner)
    {
        weaponManager = owner.GetComponent<WeaponManager>();
    }

    protected override void ApplyEffect()
    {
        if (weaponManager == null) return;
        weaponManager.SetDamageMultiplier(DamageMultiplier);
    }
}
