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
        public Image portrait;
        public Image startingWeapon;
        [NonSerialized] public Color originalColor;
    }

    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private RectTransform content;
    [SerializeField] private CharacterCard[] cards;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI selectedCharacterName;
    [SerializeField] private Image selectedPortrait;
    [SerializeField] private Image selectedStartingWeapon;
    [SerializeField] private TextMeshProUGUI selectedDescription;

    [SerializeField] private Color selectedColor = new(0.55f, 0.55f, 0f, 1f);
    [SerializeField] private string gameSceneName = "GameScene";

    private bool isStarting;

    private void Awake()
    {
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].background != null)
                    cards[i].originalColor = cards[i].background.color;
            }
        }
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

            if (card.characterName != null) card.characterName.text = data.displayName;
        }
    }

    public void SelectCharacter(int playerId)
    {
        if (isStarting || cards == null) return;

        CharacterCard selectedCard = null;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].playerId != playerId) continue;

            selectedCard = cards[i];
            break;
        }

        if (selectedCard == null) return;

        if (DataManager.Instance.SelectedPlayerId != playerId && SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.UIClick, false);

        DataManager.Instance.SelectPlayer(playerId);
        UpdateSelection(selectedCard);
    }

    private void UpdateSelection(CharacterCard selectedCard)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            CharacterCard card = cards[i];

            if (card.background == null) continue;

            card.background.color = card == selectedCard ? selectedColor : card.originalColor;
        }

        PlayerData data = DataManager.Instance.Players.Get(selectedCard.playerId);
        if (selectedCharacterName != null) selectedCharacterName.text = data.displayName;
        if (selectedDescription != null) selectedDescription.text = data.abilityDescription;
        if (selectedPortrait != null)
            selectedPortrait.sprite = selectedCard.portrait != null ? selectedCard.portrait.sprite : null;
        if (selectedStartingWeapon != null)
            selectedStartingWeapon.sprite = selectedCard.startingWeapon != null ? selectedCard.startingWeapon.sprite : null;
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
