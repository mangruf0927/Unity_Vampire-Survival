using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : Singleton<SoundManager>
{
    public const string MagicWandAttack = "MagicWandAttack";
    public const string KnifeAttack = "KnifeAttack";
    public const string KingBibleAttack = "KingBibleAttack";
    public const string WhipAttack = "WhipAttack";
    public const string EnemyHit = "EnemyHit";
    public const string EnemyDeath = "EnemyDeath";
    public const string PlayerHit = "PlayerHit";
    public const string ExperiencePickup = "ExperiencePickup";
    public const string LevelUp = "LevelUp";
    public const string GameOver = "GameOver";
    public const string StageCompleted = "StageCompleted";
    public const string UIClick = "UIClick";

    [Serializable]
    public class Sound
    {
        public string soundName;
        public AudioClip audioClip;
        [Min(0f)] public float minimumInterval;
    }

    [Header("BGM Settings")]
    public Sound[] bgmList = Array.Empty<Sound>();
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.3f;
    private AudioSource bgmSource;

    [Header("SFX Settings")]
    public Sound[] sfxList = Array.Empty<Sound>();
    [Min(1)] public int channels = 16;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    private AudioSource[] sfxSources;
    private readonly Dictionary<string, Sound> bgmSounds = new();
    private readonly Dictionary<string, Sound> sfxSounds = new();
    private readonly Dictionary<string, float> nextPlayTimes = new();
    private int channelIndex;
    private bool initialized;
    private bool gameplayPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        ResetSingleton();
    }

    protected override void Awake()
    {
        base.Awake();
        if (IsInstance) InitializeSound();
    }

    private void Start()
    {
        if (IsInstance) PlayBGM(SceneManager.GetActiveScene().name);
    }

    private void InitializeSound()
    {
        if (initialized) return;
        GameObject bgmObject = new("BGM Player");
        bgmObject.transform.SetParent(transform, false);
        bgmSource = bgmObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.volume = Mathf.Clamp01(bgmVolume);

        GameObject sfxObject = new("SFX Player");
        sfxObject.transform.SetParent(transform, false);
        sfxSources = new AudioSource[Mathf.Max(1, channels)];
        for (int i = 0; i < sfxSources.Length; i++)
        {
            AudioSource source = sfxObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = Mathf.Clamp01(sfxVolume);
            sfxSources[i] = source;
        }
        BuildCatalog(bgmList, bgmSounds);
        BuildCatalog(sfxList, sfxSounds);
        SceneManager.sceneLoaded += OnSceneLoaded;
        initialized = true;
    }

    private void BuildCatalog(Sound[] list, Dictionary<string, Sound> catalog)
    {
        catalog.Clear();
        if (list == null) return;
        for (int i = 0; i < list.Length; i++)
        {
            Sound sound = list[i];
            if (sound == null) continue;
            if (string.IsNullOrWhiteSpace(sound.soundName))
            {
                Debug.LogWarning($"SoundManager: {i}번째 사운드 이름이 비어 있습니다.", this);
                continue;
            }
            if (catalog.ContainsKey(sound.soundName))
            {
                Debug.LogWarning($"SoundManager: 중복 사운드 이름 {sound.soundName}을 건너뜁니다.", this);
                continue;
            }
            // Clip이 비어 있어도 등록해 두면 Inspector에서 나중에 연결할 수 있다.
            catalog.Add(sound.soundName, sound);
        }
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllSFX();
        gameplayPaused = false;
        nextPlayTimes.Clear();
        StopBGM();
        PlayBGM(scene.name);
    }

    public void PlayBGM(string name)
    {
        InitializeSound();
        if (string.IsNullOrEmpty(name) || !bgmSounds.TryGetValue(name, out Sound sound) || sound.audioClip == null) return;
        if (bgmSource.clip == sound.audioClip && bgmSource.isPlaying) return;
        bgmSource.clip = sound.audioClip;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySFX(string name, bool gameplaySound = true)
    {
        InitializeSound();
        if (gameplaySound && (gameplayPaused || Time.timeScale <= 0f)) return;
        if (string.IsNullOrEmpty(name) || !sfxSounds.TryGetValue(name, out Sound sound) || sound.audioClip == null) return;
        float now = Time.unscaledTime;
        if (nextPlayTimes.TryGetValue(name, out float next) && now < next) return;

        for (int i = 0; i < sfxSources.Length; i++)
        {
            int index = (channelIndex + i) % sfxSources.Length;
            AudioSource source = sfxSources[index];
            if (source.isPlaying) continue;
            source.clip = sound.audioClip;
            source.Play();
            channelIndex = (index + 1) % sfxSources.Length;
            nextPlayTimes[name] = now + Mathf.Max(0f, sound.minimumInterval);
            return;
        }
        // 모든 채널을 사용 중이면 재생 중인 소리를 끊지 않고 새 요청을 생략한다.
    }

    public void SetGameplayPaused(bool paused)
    {
        gameplayPaused = paused;
        if (paused) StopAllSFX();
    }

    public void StopAllSFX()
    {
        if (sfxSources == null) return;
        for (int i = 0; i < sfxSources.Length; i++)
            if (sfxSources[i] != null) sfxSources[i].Stop();
    }

    public void ChangeBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
    }

    public void ChangeSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSources == null) return;
        for (int i = 0; i < sfxSources.Length; i++) sfxSources[i].volume = sfxVolume;
    }

    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;

    private void OnValidate()
    {
        if (!initialized) return;
        BuildCatalog(bgmList, bgmSounds);
        BuildCatalog(sfxList, sfxSounds);
        ChangeBGMVolume(bgmVolume);
        ChangeSFXVolume(sfxVolume);
    }

    protected override void OnDestroy()
    {
        if (initialized) SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }
}
