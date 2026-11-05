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

    public void Show(IReadOnlyList<Weapon> choices)
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
            Weapon weapon = choices[i];
            card.title.text = weapon.DisplayName;
            card.status.text = weapon.IsOwned ? "Lv." + (weapon.Level + 1) : "신규 !";
            card.description.text = weapon.GetChoiceDescription();
            if (card.icon != null)
            {
                card.icon.sprite = weapon.Icon;
                card.icon.enabled = weapon.Icon != null;
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
