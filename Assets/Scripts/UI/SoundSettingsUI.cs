using UnityEngine;
using UnityEngine.UI;

public class SoundSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private SoundManager soundManager;

    private void Start()
    {
        RefreshVolumes();
    }

    private void OnEnable()
    {
        soundManager = SoundManager.Instance;
        if (soundManager == null) return;

        RefreshVolumes();
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(ChangeBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(ChangeSFXVolume);
    }

    private void OnDisable()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(ChangeBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(ChangeSFXVolume);
        if (soundManager != null) soundManager.SaveVolumeSettings();
    }

    private void ChangeBGMVolume(float volume)
    {
        if (soundManager != null) soundManager.ChangeBGMVolume(volume);
    }

    private void RefreshVolumes()
    {
        if (soundManager == null) return;
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(soundManager.GetBGMVolume());
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(soundManager.GetSFXVolume());
    }

    private void ChangeSFXVolume(float volume)
    {
        if (soundManager != null) soundManager.ChangeSFXVolume(volume);
    }
}
