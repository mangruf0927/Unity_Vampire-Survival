using UnityEngine;

public abstract class Accessory : MonoBehaviour, ILevelUpOption
{
    [SerializeField] private Sprite icon;
    [SerializeField, Min(1)] private int dataId = 1;

    private AccessoryData Data => DataManager.Instance.Accessories.Get(dataId);
    protected float BonusPerLevel => Data.bonusPerLevel;
    private bool isOwned;
    private int level;

    public int DataId => dataId;
    public bool IsOwned => isOwned;
    public int Level => level;
    public bool CanUpgrade => !isOwned || level < 5;
    public Sprite Icon => icon;

    public string DisplayName => Data.displayName;

    public string GetChoiceDescription()
    {
        return Data.choiceDescription.Replace("{bonusPercent}", (BonusPerLevel * 100f).ToString("0.#"));
    }

    public void Acquire()
    {
        if (isOwned) return;
        isOwned = true;
        level = 1;
        ApplyEffect();
    }

    public void LevelUp()
    {
        if (!isOwned || level >= 5) return;
        level++;
        ApplyEffect();
    }

    public abstract void Initialize(Transform owner);
    protected abstract void ApplyEffect();
}
