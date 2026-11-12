using UnityEngine;
using UnityEngine.UI;

public class OwnedItemsHUD : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private AccessoryManager accessoryManager;
    [SerializeField] private Image[] weaponIcons;
    [SerializeField] private Image[] accessoryIcons;
    [SerializeField] private Sprite placeholderIcon;
    [SerializeField] private Color weaponPlaceholderColor = new Color(0.35f, 0.75f, 1f);
    [SerializeField] private Color accessoryPlaceholderColor = new Color(1f, 0.75f, 0.35f);

    private void OnEnable()
    {
        if (weaponManager != null) weaponManager.OwnedItemsChanged += Refresh;
        if (accessoryManager != null) accessoryManager.OwnedItemsChanged += Refresh;
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (weaponManager != null) weaponManager.OwnedItemsChanged -= Refresh;
        if (accessoryManager != null) accessoryManager.OwnedItemsChanged -= Refresh;
    }

    private void Refresh()
    {
        EnsureAccessorySlots();
        if (weaponIcons != null)
        {
            for (int i = 0; i < weaponIcons.Length; i++)
            {
                Weapon item = weaponManager != null && i < weaponManager.OwnedWeapons.Count
                    ? weaponManager.OwnedWeapons[i] : null;
                SetIcon(weaponIcons[i], item, weaponPlaceholderColor);
            }
        }
        if (accessoryIcons != null)
        {
            for (int i = 0; i < accessoryIcons.Length; i++)
            {
                Accessory item = accessoryManager != null && i < accessoryManager.OwnedAccessories.Count
                    ? accessoryManager.OwnedAccessories[i] : null;
                // 기본 3칸은 비어 있어도 유지하고, 추가 슬롯은 획득한 경우에만 표시한다.
                int baseSlots = accessoryManager != null ? accessoryManager.MaximumAccessories : 3;
                if (accessoryIcons[i] != null)
                    accessoryIcons[i].transform.parent.gameObject.SetActive(i < baseSlots || item != null);
                SetIcon(accessoryIcons[i], item, accessoryPlaceholderColor);
            }
        }
    }

    private void EnsureAccessorySlots()
    {
        if (accessoryManager == null || accessoryIcons == null || accessoryIcons.Length == 0) return;
        int required = Mathf.Max(accessoryManager.MaximumAccessories, accessoryManager.OwnedCount);
        if (required <= accessoryIcons.Length) return;

        int previousCount = accessoryIcons.Length;
        Image template = accessoryIcons[previousCount - 1];
        if (template == null) return;
        RectTransform templateSlot = (RectTransform)template.transform.parent;
        float spacing = templateSlot.rect.width + 6f;
        if (previousCount > 1 && accessoryIcons[previousCount - 2] != null)
            spacing = templateSlot.anchoredPosition.x
                - ((RectTransform)accessoryIcons[previousCount - 2].transform.parent).anchoredPosition.x;

        System.Array.Resize(ref accessoryIcons, required);
        for (int i = previousCount; i < required; i++)
        {
            RectTransform slot = Instantiate(templateSlot, templateSlot.parent);
            slot.name = "AccessorySlot" + (i + 1);
            slot.anchoredPosition = templateSlot.anchoredPosition + new Vector2(spacing * (i - previousCount + 1), 0f);
            accessoryIcons[i] = slot.GetChild(template.transform.GetSiblingIndex()).GetComponent<Image>();
        }
    }

    private void SetIcon(Image image, ILevelUpOption item, Color placeholderColor)
    {
        if (image == null) return;
        bool hasIcon = item != null && item.Icon != null;
        Sprite sprite = hasIcon ? item.Icon : placeholderIcon;

        image.sprite = sprite != null ? sprite : null;
        image.enabled = item != null && sprite != null;
        image.color = hasIcon ? Color.white : placeholderColor;
    }
}
