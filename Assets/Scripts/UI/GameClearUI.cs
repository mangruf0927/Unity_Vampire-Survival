using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameClearUI : MonoBehaviour
{
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (gameClearPanel == null) return;
        gameClearPanel.SetActive(true);

        if (exitButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(exitButton.gameObject);
    }

    public void Hide()
    {
        if (gameClearPanel != null) gameClearPanel.SetActive(false);
    }
}
