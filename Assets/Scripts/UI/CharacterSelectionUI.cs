using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [Serializable]
    private class CharacterCard
    {
        public int playerId;
        public Button button;
        public Image background;
        public TextMeshProUGUI characterName;
        public TextMeshProUGUI startingWeapon;
        public TextMeshProUGUI description;
    }

    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private RectTransform content;
    [SerializeField] private CharacterCard[] cards;
    [SerializeField] private Button startButton;

    [SerializeField] private Color selectedColor = new(0.43f, 0.36f, 0.12f, 1f);
    [SerializeField] private Color normalColor = new(0.12f, 0.15f, 0.22f, 1f);
    [SerializeField] private string gameSceneName = "GameScene";

    private bool isStarting;

    private void Awake()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        FitContent();
    }

    public void Show()
    {
        if (selectionPanel == null || cards == null || cards.Length == 0) return;

        isStarting = false;
        selectionPanel.SetActive(true);

        Canvas.ForceUpdateCanvases();
        FitContent();
        UpdateCards();

        SelectCharacter(cards[0].playerId);

        if (startButton != null) startButton.interactable = true;

        if (EventSystem.current != null && cards[0].button != null)
            EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);
    }

    private void UpdateCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            CharacterCard card = cards[i];
            PlayerData data = DataManager.Instance.Players.Get(card.playerId);

            card.characterName.text = data.displayName;
            card.startingWeapon.text = "시작 무기: " + DataManager.Instance.Weapons.Get(data.weaponId).displayName;
            card.description.text = data.abilityDescription;
        }
    }

    public void SelectCharacter(int playerId)
    {
        if (isStarting || cards == null) return;

        bool registered = false;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].playerId != playerId) continue;

            registered = true;
            break;
        }

        if (!registered) return;

        if (DataManager.Instance.SelectedPlayerId != playerId && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.UIClick, false);

        DataManager.Instance.SelectPlayer(playerId);
        UpdateSelection(playerId);
    }

    private void UpdateSelection(int playerId)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            CharacterCard card = cards[i];

            if (card.background == null) continue;

            card.background.color = card.playerId == playerId ? selectedColor : normalColor;
        }
    }

    private void FitContent()
    {
        if (content == null || selectionPanel == null) return;

        RectTransform panel = selectionPanel.transform as RectTransform;
        if (panel == null || panel.rect.width <= 0f || panel.rect.height <= 0f) return;

        float scale = Mathf.Min(1f, Mathf.Min((panel.rect.width - 32f) / content.sizeDelta.x, (panel.rect.height - 32f) / content.sizeDelta.y));

        content.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    public void StartGame()
    {
        if (isStarting) return;

        isStarting = true;

        if (startButton != null) startButton.interactable = false;

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }
}
