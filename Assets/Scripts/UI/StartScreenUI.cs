
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI startText;
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.8f;
    [SerializeField] private string nextSceneName = "GameScene";

    private float blinkTimer;
    private bool isStarting;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        blinkTimer = 0f;
        isStarting = false;

        if (startText != null) startText.alpha = 1f;
    }

    private void Update()
    {
        if (isStarting) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            isStarting = true;
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        UpdateBlink();
    }

    private void UpdateBlink()
    {
        if (startText == null) return;

        blinkTimer += Time.unscaledDeltaTime;

        if (blinkTimer < blinkInterval) return;

        blinkTimer = 0f;
        startText.alpha = startText.alpha > 0f ? 0f : 1f;
    }
}
