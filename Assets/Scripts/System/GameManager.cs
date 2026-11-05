using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, GameOver }

public class GameManager : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private PlayerHPBar playerHPBar;

    private bool isLeaving;
    public GameState State { get; private set; } = GameState.Playing;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (State != GameState.Playing) return;
        if (gameOverUI != null) gameOverUI.Hide();
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
        if (State == GameState.GameOver) Time.timeScale = 1f;
    }

    public void ReturnToStartScene()
    {
        if (State != GameState.GameOver || isLeaving) return;
        isLeaving = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }

    public void Notify()
    {
        if (State != GameState.Playing || playerHealth == null || !playerHealth.IsDead) return;

        State = GameState.GameOver;
        Time.timeScale = 0f;

        if (playerHPBar != null) playerHPBar.SetVisible(false);
        if (gameOverUI != null) gameOverUI.Show();
        playerHealth.gameObject.SetActive(false);
        Destroy(playerHealth.gameObject);
    }
}
