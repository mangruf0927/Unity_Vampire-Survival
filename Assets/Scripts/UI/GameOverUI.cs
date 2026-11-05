using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject playerHPBar;
    [SerializeField] private Button exitButton;

    private bool isGameOver;
    private bool isLeaving;

    private void Awake()
    {
        if (playerHealth == null || resultPanel == null || exitButton == null)
        {
            Debug.LogWarning("GameOverUI의 Player Health, Result Panel, Exit Button, Exit Label을 지정해 주세요.", this);
            enabled = false;
            return;
        }
        resultPanel.SetActive(false);

    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.AddObserver(this);
        exitButton.onClick.AddListener(ExitToStartScene);
        Notify();
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.RemoveObserver(this);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitToStartScene);
    }

    public void Notify()
    {
        if (isGameOver || playerHealth == null || !playerHealth.IsDead) return;

        isGameOver = true;
        Time.timeScale = 0f;
        resultPanel.SetActive(true);
        if (playerHPBar != null) playerHPBar.SetActive(false);
        playerHealth.gameObject.SetActive(false);
        Destroy(playerHealth.gameObject);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
    }

    private void ExitToStartScene()
    {
        if (!isGameOver || isLeaving) return;
        isLeaving = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }

    private void OnDestroy()
    {
        if (isGameOver) Time.timeScale = 1f;
    }
}
