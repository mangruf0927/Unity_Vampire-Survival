using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Playing, GameOver, Completed, Paused }

public class GameManager : MonoBehaviour, IObserver
{
    private const float StageDuration = 600f;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TimeSystem timeSystem;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private GameClearUI gameClearUI;
    [SerializeField] private PlayerHPBar playerHPBar;
    [SerializeField] private PauseUI pauseUI;

    private bool isLeaving;
    private bool isDeathPending;
    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (State != GameState.Playing) return;
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
        Notify();
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.RemoveObserver(this);
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
        if ((State == GameState.Playing || State == GameState.Paused)
            && playerHealth != null && playerHealth.IsDead)
            isDeathPending = true;
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
        if (State != GameState.Playing && State != GameState.Paused) return;

        if (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)
            FinishGame(GameState.Completed);
        else if (isDeathPending)
            FinishGame(GameState.GameOver);
    }

    private void FinishGame(GameState result)
    {
        if (State != GameState.Playing && State != GameState.Paused) return;
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
