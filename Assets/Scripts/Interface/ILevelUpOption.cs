using UnityEngine;

public interface ILevelUpOption
{
    bool IsOwned { get; }
    bool CanUpgrade { get; }
    int Level { get; }
    string DisplayName { get; }
    Sprite Icon { get; }
    string GetChoiceDescription();
}
