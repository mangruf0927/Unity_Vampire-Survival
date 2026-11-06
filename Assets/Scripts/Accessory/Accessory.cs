using UnityEngine;

public abstract class Accessory : MonoBehaviour, ILevelUpOption
{
    [SerializeField] private Sprite icon;
    private bool isOwned;
    private int level;

    public bool IsOwned => isOwned;
    public int Level => level;
    public bool CanUpgrade => !isOwned || level < 5;
    public Sprite Icon => icon;
    public abstract string DisplayName { get; }
    public abstract string GetChoiceDescription();
    public abstract void Initialize(Transform owner);

    public void Acquire()
    {
        if (isOwned) return;
        isOwned = true;
        level = 1;
        ApplyEffect();
    }

    [ContextMenu("Level Up (Play Mode)")]
    public void LevelUp()
    {
        if (!Application.isPlaying || !isOwned || level >= 5) return;
        level++;
        ApplyEffect();
    }

    protected abstract void ApplyEffect();
}
