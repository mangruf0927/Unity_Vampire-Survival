using System.Collections.Generic;
using UnityEngine;

public class PlayerExperience : MonoBehaviour, ISubject
{
    [SerializeField] private SphereCollider attractionCollider;

    private int initialExp = 5;
    private int expGrowth = 10;
    private float attractionRange = 1f;
    private float attractionSpeed = 10f;

    private int level = 1;
    private int currentExperience;
    private float rangeMultiplier = 1f;

    private readonly List<IObserver> observers = new();

    public int Level => level;
    public int CurrentExperience => currentExperience;
    public int RequiredExperience => Mathf.Max(1, initialExp + (level - 1) * expGrowth);

    private void Awake()
    {
        PlayerData data = DataManager.Instance.SelectedPlayer;
        initialExp = data.initialExp;
        expGrowth = data.expGrowth;
        attractionRange = data.attractionRange;
        attractionSpeed = data.attractionSpeed;

        UpdateRange();
    }

    private void OnValidate()
    {
        UpdateRange();
    }

    private void UpdateRange()
    {
        if (attractionCollider == null) return;
        attractionCollider.radius = Mathf.Max(0.01f, attractionRange * rangeMultiplier);
    }

    public void SetRangeMultiplier(float multiplier)
    {
        rangeMultiplier = Mathf.Max(0f, multiplier);
        UpdateRange();
    }

    public void TryAttractGem(Collider sensor, ExperienceGem gem)
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || attractionCollider == null || sensor != attractionCollider || gem == null)
            return;

        gem.AttractTo(this, attractionSpeed);
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
}
