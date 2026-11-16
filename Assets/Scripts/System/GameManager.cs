using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Playing, GameOver, Completed, Paused, ChoosingUpgrade }

public class GameManager : MonoBehaviour, IObserver
{
    private const float StageDuration = 600f;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private AccessoryManager accessoryManager;
    [SerializeField] private TimeSystem timeSystem;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private GameClearUI gameClearUI;
    [SerializeField] private PlayerHPBar playerHPBar;
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private LevelUpUI levelUpUI;

    private readonly List<ILevelUpOption> choices = new();

    private int lastPlayerLevel = 1;
    private int pendingChoices;
    private bool isLeaving;
    private bool isDeathPending;

    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        SetPaused(false);
    }

    private void Start()
    {
        if (State != GameState.Playing) return;

        if (levelUpUI != null) levelUpUI.Hide();
        if (gameOverUI != null) gameOverUI.Hide();
        if (gameClearUI != null) gameClearUI.Hide();
        if (playerHPBar != null) playerHPBar.SetVisible(true);

        if (pauseUI != null)
        {
            pauseUI.Hide();
            pauseUI.SetPauseAvailable(true);
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.AddObserver(this);
        if (playerExperience != null) playerExperience.AddObserver(this);
        Notify();
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.RemoveObserver(this);
        if (playerExperience != null) playerExperience.RemoveObserver(this);
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (State == GameState.Playing) PauseGame();
        else if (State == GameState.Paused) ResumeGame();
    }

    private void LateUpdate()
    {
        if (State == GameState.GameOver || State == GameState.Completed || isLeaving) return;

        if (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)
            FinishGame(GameState.Completed);
        else if (isDeathPending)
            FinishGame(GameState.GameOver);
        else if (State == GameState.Playing)
            ShowNextChoice();
    }

    private void OnDestroy()
    {
        if (State != GameState.Playing) Time.timeScale = 1f;
    }

    public void Notify()
    {
        if (State == GameState.GameOver || State == GameState.Completed || isLeaving) return;

        if (playerHealth != null && playerHealth.IsDead) isDeathPending = true;

        if (playerExperience != null)
        {
            pendingChoices += Mathf.Max(0, playerExperience.Level - lastPlayerLevel);
            lastPlayerLevel = playerExperience.Level;
        }
    }

    public void ChooseOption(int index)
    {
        if (State != GameState.ChoosingUpgrade || isLeaving || index < 0 || index >= choices.Count) return;
        if (isDeathPending || (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)) return;
        if (!ApplyChoice(choices[index])) return;

        pendingChoices--;
        choices.Clear();

        if (levelUpUI != null) levelUpUI.Hide();

        State = GameState.Playing;

        if (!ShowNextChoice())
        {
            SetPaused(false);
            if (pauseUI != null) pauseUI.SetPauseAvailable(true);
        }
    }

    private bool ApplyChoice(ILevelUpOption choice)
    {
        if (choice is Weapon weapon)
            return weaponManager != null && weaponManager.AcquireOrUpgrade(weapon);

        if (choice is Accessory accessory)
            return accessoryManager != null && accessoryManager.AcquireOrUpgrade(accessory);

        return false;
    }

    private bool ShowNextChoice()
    {
        if (pendingChoices <= 0 || levelUpUI == null) return false;

        choices.Clear();

        if (weaponManager != null) weaponManager.AddChoices(choices);
        if (accessoryManager != null) accessoryManager.AddChoices(choices);

        SelectChoices();

        if (choices.Count == 0)
        {
            pendingChoices = 0;
            return false;
        }

        State = GameState.ChoosingUpgrade;
        SetPaused(true);

        SoundManager.Instance?.PlaySFX(SoundManager.LevelUp, false);

        if (pauseUI != null) pauseUI.SetPauseAvailable(false);

        levelUpUI.Show(choices);
        return true;
    }

    private void SelectChoices()
    {
        int capacity = levelUpUI.Capacity;
        if (choices.Count <= capacity) return;

        for (int i = 0; i < capacity; i++)
        {
            int index = Random.Range(i, choices.Count);
            (choices[index], choices[i]) = (choices[i], choices[index]);
        }

        choices.RemoveRange(capacity, choices.Count - capacity);
    }

    public void PauseGame()
    {
        if (State != GameState.Playing || isLeaving) return;
        if (isDeathPending || (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)) return;

        State = GameState.Paused;
        SetPaused(true);

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.UIClick, false);
        if (pauseUI != null) pauseUI.Show();
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused || isLeaving) return;

        State = GameState.Playing;
        SetPaused(false);

        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.UIClick, false);
        if (pauseUI != null) pauseUI.Hide();
    }

    private void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
        if (SoundManager.Instance != null) SoundManager.Instance.SetGameplayPaused(paused);
    }

    private void FinishGame(GameState result)
    {
        if (State == GameState.GameOver || State == GameState.Completed || isLeaving) return;

        pendingChoices = 0;
        choices.Clear();

        if (levelUpUI != null) levelUpUI.Hide();

        State = result;
        SetPaused(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopBGM();
            SoundManager.Instance.PlaySFX(result == GameState.Completed ? SoundManager.StageCompleted : SoundManager.GameOver, false);
        }
        ShowResultUI(result);

        if (result == GameState.GameOver && playerHealth != null)
        {
            if (playerHealth.TryGetComponent(out PlayerCharacter character) && character.TryPlayDeathAnimation())
                StartCoroutine(RemovePlayerAfterDeath(character));
            else
            {
                playerHealth.gameObject.SetActive(false);
                Destroy(playerHealth.gameObject);
            }
        }
    }

    private IEnumerator RemovePlayerAfterDeath(PlayerCharacter character)
    {
        while (character != null && !character.IsDeathAnimationComplete)
            yield return null;

        if (playerHealth == null) yield break;
        playerHealth.gameObject.SetActive(false);
        Destroy(playerHealth.gameObject);
    }

    private void ShowResultUI(GameState result)
    {
        if (pauseUI != null)
        {
            pauseUI.Hide();
            pauseUI.SetPauseAvailable(false);
        }

        if (playerHPBar != null) playerHPBar.SetVisible(false);

        if (result == GameState.Completed)
        {
            if (gameOverUI != null) gameOverUI.Hide();
            if (gameClearUI != null) gameClearUI.Show();
        }
        else
        {
            if (gameClearUI != null) gameClearUI.Hide();
            if (gameOverUI != null) gameOverUI.Show();
        }
    }

    public void ReturnToStartScene()
    {
        if (State == GameState.Playing || isLeaving) return;

        isLeaving = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }
}
