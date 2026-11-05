using System.Collections.Generic;
using UnityEngine;

public class TimeSystem : MonoBehaviour, ISubject
{
    [SerializeField, Min(0f)] private float startTime = 0f;

    private readonly List<IObserver> ObserverList = new();

    private int minutes = -1;
    private int seconds = -1;
    private float timeElapsed;

    public int Minutes => minutes;
    public int Seconds => seconds;

    private void Awake()
    {
        timeElapsed = Mathf.Max(0f, startTime);
        UpdateTimer();
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        timeElapsed += Time.deltaTime;
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        int totalSeconds = Mathf.FloorToInt(timeElapsed);
        int newMinutes = totalSeconds / 60;
        int newSeconds = totalSeconds % 60;

        if (minutes == newMinutes && seconds == newSeconds) return;

        minutes = newMinutes;
        seconds = newSeconds;

        NotifyObservers();
    }

    public void AddObserver(IObserver observer)
    {
        if (observer != null && !ObserverList.Contains(observer)) ObserverList.Add(observer);
    }

    public void RemoveObserver(IObserver observer)
    {
        if (observer == null) return;
        ObserverList.Remove(observer);
    }

    public void NotifyObservers()
    {
        IObserver[] snapshot = ObserverList.ToArray();
        foreach (IObserver observer in snapshot)
        {
            observer.Notify();
        }
    }
}
