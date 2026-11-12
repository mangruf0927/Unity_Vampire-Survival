using UnityEngine;

public class Wings : Accessory
{
    private PlayerMovement playerMovement;

    public float MoveSpeedMultiplier => 1f + BonusPerLevel * Level;

    public override void Initialize(Transform owner)
    {
        playerMovement = owner.GetComponent<PlayerMovement>();
    }

    protected override void ApplyEffect()
    {
        if (playerMovement == null) return;
        playerMovement.SetMoveSpeedMultiplier(MoveSpeedMultiplier);
    }
}
