using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(true);
        if (resumeButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Hide()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        if (EventSystem.current != null && pausePanel != null
            && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(pausePanel.transform))
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void SetPauseAvailable(bool available)
    {
        if (pauseButton != null) pauseButton.gameObject.SetActive(available);
    }
}
