using UnityEngine;

public class Attractorb : Accessory
{
    private PlayerExperience playerExperience;

    public float AttractionRangeMultiplier => 1f + BonusPerLevel * Level;

    public override void Initialize(Transform owner)
    {
        playerExperience = owner.GetComponent<PlayerExperience>();
    }

    protected override void ApplyEffect()
    {
        if (playerExperience == null) return;

        playerExperience.SetAttractionRangeMultiplier(AttractionRangeMultiplier);
    }
}
