
using UnityEngine;
using UnityEngine.UI;

public class OwnedItemsHUD : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private AccessoryManager accessoryManager;

    [SerializeField] private Image[] weaponIcons;
    [SerializeField] private Image[] accessoryIcons;
    [SerializeField] private Sprite placeholderIcon;

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
        RefreshWeapons();
        RefreshAccessories();
    }

    private void RefreshWeapons()
    {
        if (weaponIcons == null) return;

        for (int i = 0; i < weaponIcons.Length; i++)
        {
            Weapon weapon = weaponManager != null && i < weaponManager.OwnedWeapons.Count ? weaponManager.OwnedWeapons[i] : null;

            SetIcon(weaponIcons[i], weapon);
        }
    }

    private void RefreshAccessories()
    {
        if (accessoryIcons == null) return;

        int baseSlots = accessoryManager != null ? accessoryManager.MaximumAccessories : 3;

        for (int i = 0; i < accessoryIcons.Length; i++)
        {
            Accessory accessory = accessoryManager != null && i < accessoryManager.OwnedAccessories.Count ? accessoryManager.OwnedAccessories[i] : null;

            Image icon = accessoryIcons[i];
            if (icon == null) continue;

            icon.transform.parent.gameObject.SetActive(i < baseSlots || accessory != null);
            SetIcon(icon, accessory);
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
        {
            RectTransform previousSlot = (RectTransform)accessoryIcons[previousCount - 2].transform.parent;
            spacing = templateSlot.anchoredPosition.x - previousSlot.anchoredPosition.x;
        }

        System.Array.Resize(ref accessoryIcons, required);

        for (int i = previousCount; i < required; i++)
        {
            RectTransform slot = Instantiate(templateSlot, templateSlot.parent);
            slot.name = $"AccessorySlot{i + 1}";
            slot.anchoredPosition = templateSlot.anchoredPosition + new Vector2(spacing * (i - previousCount + 1), 0f);

            accessoryIcons[i] = slot.GetChild(template.transform.GetSiblingIndex()).GetComponent<Image>();
        }
    }

    private void SetIcon(Image image, ILevelUpOption item)
    {
        if (image == null) return;

        if (item == null)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = item.Icon != null ? item.Icon : placeholderIcon;

        image.enabled = image.sprite != null;
        image.color = Color.white;
    }
}
