using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Playing, GameOver, Completed, Paused, ChoosingUpgrade }

public class GameManager : MonoBehaviour, IObserver
{
    private const float StageDuration = 600f;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TimeSystem timeSystem;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private GameClearUI gameClearUI;
    [SerializeField] private PlayerHPBar playerHPBar;
    [SerializeField] private PauseUI pauseUI;

    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private Weapon startingWeapon;
    [SerializeField] private Weapon[] weapons;
    [SerializeField] private LevelUpUI levelUpUI;

    private readonly List<Weapon> choices = new();
    private int lastPlayerLevel = 1;
    private int pendingChoices;
    private bool isLeaving;
    private bool isDeathPending;
    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (startingWeapon != null) startingWeapon.Acquire();
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

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (State == GameState.Playing) PauseGame();
        else if (State == GameState.Paused) ResumeGame();
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

    private void OnDestroy()
    {
        if (State != GameState.Playing) Time.timeScale = 1f;
    }

    public void ReturnToStartScene()
    {
        if (State == GameState.Playing || isLeaving) return;
        isLeaving = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }

    public void Notify()
    {
        if (State == GameState.GameOver || State == GameState.Completed || isLeaving) return;
        if (playerHealth != null && playerHealth.IsDead) isDeathPending = true;
        if (playerExperience != null)
        {
            // 경험치 알림의 최종 레벨과 이전 레벨의 차이를 선택 횟수로 보관한다.
            pendingChoices += Mathf.Max(0, playerExperience.Level - lastPlayerLevel);
            lastPlayerLevel = playerExperience.Level;
        }
    }

    public void ChooseWeapon(int index)
    {
        if (State != GameState.ChoosingUpgrade || isLeaving || index < 0 || index >= choices.Count) return;
        if (isDeathPending || (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)) return;
        Weapon weapon = choices[index];
        if (weapon == null || !weapon.CanUpgrade) return;

        if (weapon.IsOwned) weapon.LevelUp();
        else weapon.Acquire();
        pendingChoices--;
        choices.Clear();
        if (levelUpUI != null) levelUpUI.Hide();
        State = GameState.Playing;
        // 연속 레벨업 중에는 시간을 재개하지 않고 다음 선택으로 진행한다.
        if (!ShowNextChoice())
        {
            Time.timeScale = 1f;
            if (pauseUI != null) pauseUI.SetPauseAvailable(true);
        }
    }

    private bool ShowNextChoice()
    {
        if (pendingChoices <= 0 || levelUpUI == null) return false;
        choices.Clear();
        for (int i = 0; weapons != null && i < weapons.Length && choices.Count < levelUpUI.Capacity; i++)
        {
            Weapon weapon = weapons[i];
            if (weapon != null && weapon.CanUpgrade && !choices.Contains(weapon)) choices.Add(weapon);
        }
        if (choices.Count == 0)
        {
            pendingChoices = 0;
            return false;
        }
        State = GameState.ChoosingUpgrade;
        Time.timeScale = 0f;
        if (pauseUI != null) pauseUI.SetPauseAvailable(false);
        levelUpUI.Show(choices);
        return true;
    }

    public void PauseGame()
    {
        if (State != GameState.Playing || isLeaving) return;
        // 종료 조건이 이미 충족됐다면 일시 정지보다 종료를 우선한다.
        if (isDeathPending || (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        if (pauseUI != null) pauseUI.Show();
    }

    public void ResumeGame()
    {
        if (State != GameState.Paused || isLeaving) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        if (pauseUI != null) pauseUI.Hide();
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

    private void FinishGame(GameState result)
    {
        if (State == GameState.GameOver || State == GameState.Completed || isLeaving) return;
        pendingChoices = 0;
        choices.Clear();
        if (levelUpUI != null) levelUpUI.Hide();
        State = result;
        Time.timeScale = 0f;

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

        if (result == GameState.GameOver && playerHealth != null)
        {
            playerHealth.gameObject.SetActive(false);
            Destroy(playerHealth.gameObject);
        }
    }
}
