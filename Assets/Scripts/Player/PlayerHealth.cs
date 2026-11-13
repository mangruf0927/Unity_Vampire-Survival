using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable, ISubject
{
    private readonly List<IObserver> observers = new();

    private float maxHealth;
    private float damageInterval;
    private float currentHealth;
    private float nextDamageTime;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        PlayerData data = DataManager.Instance.SelectedPlayer;
        maxHealth = data.maxHealth;
        damageInterval = data.damageInterval;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f || Time.timeScale <= 0f || Time.time < nextDamageTime) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        nextDamageTime = Time.time + Mathf.Max(0.01f, damageInterval);

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.PlayerHit);

        if (currentHealth <= 0f) Die();
        NotifyObservers();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f || Time.timeScale <= 0f) return;

        float healedHealth = Mathf.Min(maxHealth, currentHealth + amount);
        if (healedHealth == currentHealth) return;

        currentHealth = healedHealth;
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
