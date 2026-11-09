using UnityEngine;

public class HollowHeart : Accessory
{
    [SerializeField, Min(0f)] private float healthBonusPerLevel = 0.2f;
    private PlayerHealth playerHealth;

    public override string DisplayName => "Hollow Heart";

    public override void Initialize(Transform owner)
    {
        playerHealth = owner.GetComponent<PlayerHealth>();
    }

    public override string GetChoiceDescription()
    {
        return "현재 최대 체력이 " + (healthBonusPerLevel * 100f).ToString("0.#")
            + "% 증가합니다. 매 단계 반올림하며 현재 체력은 회복하지 않습니다.";
    }

    protected override void ApplyEffect()
    {
        if (playerHealth != null) playerHealth.IncreaseMaxHealth(healthBonusPerLevel);
    }
}
