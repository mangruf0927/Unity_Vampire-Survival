using UnityEngine;

public class Wings : Accessory
{
    private float speedBonusPerLevel => BonusPerLevel;
    private PlayerMovement playerMovement;

    public override string DisplayName => "Wings";
    public float MoveSpeedMultiplier => 1f + speedBonusPerLevel * Level;

    public override void Initialize(Transform owner)
    {
        playerMovement = owner.GetComponent<PlayerMovement>();
    }

    public override string GetChoiceDescription()
    {
        return "기본 이동 속도의 " + (speedBonusPerLevel * 100f).ToString("0.#") + "%만큼 이동 속도가 증가합니다.";
    }

    protected override void ApplyEffect()
    {
        if (playerMovement != null) playerMovement.SetMoveSpeedMultiplier(MoveSpeedMultiplier);
    }
}
