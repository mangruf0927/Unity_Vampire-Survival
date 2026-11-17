using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceUI : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TextMeshProUGUI levelText;

    private void OnEnable()
    {
        if (playerExperience == null) return;
        playerExperience.AddObserver(this);
        Notify();
    }

    private void OnDisable()
    {
        if (playerExperience != null) playerExperience.RemoveObserver(this);
    }

    public void Notify()
    {
        if (playerExperience == null || experienceSlider == null || levelText == null) return;

        float ratio = (float)playerExperience.CurrentExperience / playerExperience.RequiredExperience;
        experienceSlider.SetValueWithoutNotify(Mathf.Clamp01(ratio));
        levelText.text = $"레벨 {playerExperience.Level}";
    }
}
