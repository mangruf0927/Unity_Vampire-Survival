using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    [SerializeField, Min(1)] private int firstLevelRequirement = 5;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 10;

    private int level = 1;
    private int currentExperience;

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int RequiredExperience => Mathf.Max(1, firstLevelRequirement + (level - 1) * experienceIncreasePerLevel);

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        while (amount > 0)
        {
            int remaining = RequiredExperience - currentExperience;
            if (amount < remaining)
            {
                currentExperience += amount;
                return;
            }
            amount -= remaining;
            currentExperience = 0;
            level++;
        }
    }
}
