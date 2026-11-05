using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        if (exitButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
    }

    public void Hide()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }
}
