
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartScreenUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI startText;
    [SerializeField, Min(0.01f)] private float blinkInterval = 0.8f;
    [SerializeField] private CharacterSelectionUI characterSelectionUI;

    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button optionsButton;
    [SerializeField] private TextMeshProUGUI optionsButtonText;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite backHighlightedSprite;
    [SerializeField] private Sprite backPressedSprite;

    private Sprite optionsSprite;
    private SpriteState optionsSpriteState;
    private float blinkTimer;
    private bool isStarting;

    private void Awake()
    {
        Time.timeScale = 1f;
        optionsSprite = optionsButton.image.sprite;
        optionsSpriteState = optionsButton.spriteState;
        optionsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        blinkTimer = 0f;
        isStarting = false;

        if (startText != null) startText.alpha = 1f;
    }

    private void Update()
    {
        if (isStarting || optionsPanel.activeSelf) return;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            isStarting = true;
            if (startText != null) startText.gameObject.SetActive(false);
            if (characterSelectionUI != null) characterSelectionUI.Show();
            SetBackButton(true);
            return;
        }

        UpdateBlink();
    }

    public void ToggleOptions()
    {
        if (isStarting)
        {
            if (characterSelectionUI != null) characterSelectionUI.Hide();
            isStarting = false;
            blinkTimer = 0f;
            if (startText != null)
            {
                startText.gameObject.SetActive(true);
                startText.alpha = 1f;
            }
            SetBackButton(false);
            return;
        }

        bool isOpen = !optionsPanel.activeSelf;
        optionsPanel.SetActive(isOpen);
        SetBackButton(isOpen);

        if (startText != null)
            startText.gameObject.SetActive(!isOpen && !isStarting);
    }

    private void SetBackButton(bool showBack)
    {
        optionsButtonText.text = showBack ? "뒤로" : "옵션";
        optionsButton.enabled = false;
        optionsButton.image.sprite = showBack ? backSprite : optionsSprite;

        SpriteState spriteState = optionsSpriteState;
        if (showBack)
        {
            spriteState.highlightedSprite = backHighlightedSprite;
            spriteState.pressedSprite = backPressedSprite;
        }
        optionsButton.spriteState = spriteState;
        optionsButton.enabled = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
