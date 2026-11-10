using UnityEngine;

public class Attractorb : Accessory
{
    private float attractionRangeBonusPerLevel => BonusPerLevel;
    private PlayerExperience playerExperience;

    public override string DisplayName => "Attractorb";
    public float AttractionRangeMultiplier => 1f + attractionRangeBonusPerLevel * Level;

    public override void Initialize(Transform owner)
    {
        playerExperience = owner.GetComponent<PlayerExperience>();
    }

    public override string GetChoiceDescription()
    {
        return "경험치 보석을 끌어당기는 범위가 기본값의 "
            + (attractionRangeBonusPerLevel * 100f).ToString("0.#") + "%만큼 증가합니다.";
    }

    protected override void ApplyEffect()
    {
        if (playerExperience != null)
            playerExperience.SetAttractionRangeMultiplier(AttractionRangeMultiplier);
    }
}
