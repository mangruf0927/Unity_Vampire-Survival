using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour, IObserver
{
    [SerializeField] private TimeSystem timeSystem;
    [SerializeField] private TextMeshProUGUI timerText;

    private void OnEnable()
    {
        if (timeSystem != null) timeSystem.AddObserver(this);
        Notify();
    }

    private void OnDisable()
    {
        if (timeSystem != null) timeSystem.RemoveObserver(this);
    }

    public void Notify()
    {
        if (timeSystem == null || timerText == null) return;

        timerText.text = $"{timeSystem.Minutes:00} : {timeSystem.Seconds:00}";
    }
}
