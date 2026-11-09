using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable, ISubject
{
    [SerializeField, Min(0.01f)] private float maxHealth = 100f;
    [SerializeField, Min(0.01f)] private float damageInterval = 1f;

    private float currentHealth;
    private float nextDamageTime;
    private bool isDead;
    private readonly List<IObserver> observers = new();

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
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

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f || Time.timeScale <= 0f || Time.time < nextDamageTime) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        nextDamageTime = Time.time + Mathf.Max(0.01f, damageInterval);
        if (currentHealth <= 0f) Die();
        NotifyObservers();
    }

    public void IncreaseMaxHealth(float increaseRate)
    {
        if (isDead || increaseRate <= 0f) return;
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * (1f + increaseRate)));

        NotifyObservers();
    }

    private void Die()
    {
        isDead = true;
    }
}
