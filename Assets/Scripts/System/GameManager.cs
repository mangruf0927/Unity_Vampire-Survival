using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, GameOver, Completed }

public class GameManager : MonoBehaviour, IObserver
{
    private const float StageDuration = 600f;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private TimeSystem timeSystem;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private GameClearUI gameClearUI;
    [SerializeField] private PlayerHPBar playerHPBar;

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
        if (State == GameState.Playing && playerHealth != null && playerHealth.IsDead)
            isDeathPending = true;
    }

    private void LateUpdate()
    {
        if (State != GameState.Playing) return;

        if (timeSystem != null && timeSystem.ElapsedSeconds >= StageDuration)
            FinishGame(GameState.Completed);
        else if (isDeathPending)
            FinishGame(GameState.GameOver);
    }

    private void FinishGame(GameState result)
    {
        if (State != GameState.Playing) return;
        State = result;
        Time.timeScale = 0f;

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
