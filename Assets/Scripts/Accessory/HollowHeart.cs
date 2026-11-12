using UnityEngine;

public class HollowHeart : Accessory
{
    private PlayerHealth playerHealth;


    public override void Initialize(Transform owner)
    {
        playerHealth = owner.GetComponent<PlayerHealth>();
    }

    protected override void ApplyEffect()
    {
        if (playerHealth == null) return;
        playerHealth.IncreaseMaxHealth(BonusPerLevel);
    }
}
