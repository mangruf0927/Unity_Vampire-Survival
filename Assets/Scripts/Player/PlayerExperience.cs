using System.Collections.Generic;
using UnityEngine;

public class PlayerExperience : MonoBehaviour, ISubject
{
    [SerializeField, Min(1)] private int firstLevelRequirement = 5;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 10;

    private int level = 1;
    private int currentExperience;
    private readonly List<IObserver> observers = new();

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int RequiredExperience => Mathf.Max(1, firstLevelRequirement + (level - 1) * experienceIncreasePerLevel);

    public void AddObserver(IObserver observer)
    {
        if (observer != null && !observers.Contains(observer)) observers.Add(observer);
    }

    public void RemoveObserver(IObserver observer)
    {
        observers.Remove(observer);
    }

    public void NotifyObservers()
    {
        IObserver[] snapshot = observers.ToArray();
        for (int i = 0; i < snapshot.Length; i++) snapshot[i].Notify();
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        while (amount > 0)
        {
            int remaining = RequiredExperience - currentExperience;
            if (amount < remaining)
            {
                currentExperience += amount;
                break;
            }
            amount -= remaining;
            currentExperience = 0;
            level++;
        }
        // 연속 레벨업도 최종 상태를 한 번만 알린다.
        NotifyObservers();
    }
}
