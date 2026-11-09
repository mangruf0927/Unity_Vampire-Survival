using System.Collections.Generic;
using UnityEngine;

public class PlayerExperience : MonoBehaviour, ISubject
{
    [SerializeField, Min(1)] private int firstLevelRequirement = 5;
    [SerializeField, Min(0)] private int experienceIncreasePerLevel = 10;
    [SerializeField] private SphereCollider attractionCollider;
    [SerializeField, Min(0.01f)] private float attractionRange = 1f;
    [SerializeField, Min(0f)] private float attractionSpeed = 10f;

    private int level = 1;
    private int currentExperience;
    private float attractionRangeMultiplier = 1f;
    private readonly List<IObserver> observers = new();

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int RequiredExperience => Mathf.Max(1, firstLevelRequirement + (level - 1) * experienceIncreasePerLevel);

    private void Awake()
    {
        UpdateAttractionRange();
    }

    private void OnValidate()
    {
        UpdateAttractionRange();
    }

    private void UpdateAttractionRange()
    {
        if (attractionCollider != null)
            attractionCollider.radius = Mathf.Max(0.01f, attractionRange * attractionRangeMultiplier);
    }

    public void SetAttractionRangeMultiplier(float multiplier)
    {
        attractionRangeMultiplier = Mathf.Max(0f, multiplier);
        UpdateAttractionRange();
    }

    public void TryAttractGem(Collider sensor, ExperienceGem gem)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || attractionCollider == null
            || sensor != attractionCollider || gem == null) return;

        gem.AttractTo(this, attractionSpeed);
    }

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
        NotifyObservers();
    }
}
