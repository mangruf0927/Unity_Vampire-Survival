using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    [Serializable]
    private class ChoiceCard
    {
        public Button button;
        public Image icon;
        public TextMeshProUGUI title;
        public TextMeshProUGUI status;
        public TextMeshProUGUI description;
    }

    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private ChoiceCard[] cards;
    [SerializeField, Min(1f)] private float cardSpacing = 140f;

    public int Capacity => cards == null ? 0 : cards.Length;

    private void Awake()
    {
        Hide();
    }

    public void Show(IReadOnlyList<ILevelUpOption> choices)
    {
        if (selectionPanel == null) return;
        int count = Mathf.Min(choices.Count, Capacity);
        selectionPanel.SetActive(true);
        for (int i = 0; i < Capacity; i++)
        {
            ChoiceCard card = cards[i];
            bool visible = i < count;
            card.button.gameObject.SetActive(visible);
            if (!visible) continue;
            ILevelUpOption option = choices[i];
            card.title.text = option.DisplayName;
            card.status.text = option.IsOwned ? "Lv." + (option.Level + 1) : "신규 !";
            card.description.text = option.GetChoiceDescription();
            if (card.icon != null)
            {
                card.icon.sprite = option.Icon;
                card.icon.enabled = option.Icon != null;
            }
            RectTransform rect = (RectTransform)card.button.transform;
            rect.anchoredPosition = new Vector2(0f, ((count - 1) * 0.5f - i) * cardSpacing);
        }
        if (count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(cards[0].button.gameObject);
    }

    public void Hide()
    {
        if (selectionPanel == null) return;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
            && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(selectionPanel.transform))
            EventSystem.current.SetSelectedGameObject(null);
        selectionPanel.SetActive(false);
    }
}
