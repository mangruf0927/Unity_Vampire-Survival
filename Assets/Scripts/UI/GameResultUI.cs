using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameResultUI : MonoBehaviour
{
    [System.Serializable]
    private class WeaponRow
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI weaponName;
        public TextMeshProUGUI level;
        public TextMeshProUGUI acquisitionTime;
    }

    [System.Serializable]
    private class AccessorySlot
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI level;
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI survivalTime;
    [SerializeField] private TextMeshProUGUI playerLevel;
    [SerializeField] private TextMeshProUGUI defeatedEnemies;
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private WeaponRow[] weaponRows;
    [SerializeField] private AccessorySlot[] accessorySlots;
    [SerializeField] private Button mainScreenButton;

    private void Awake() => Hide();

    public void Capture(float elapsed, int level, int kills, string playerName,
        IReadOnlyList<Weapon> weapons, IReadOnlyList<Accessory> accessories)
    {
        if (survivalTime != null) survivalTime.text = FormatTime(elapsed);
        if (playerLevel != null) playerLevel.text = level.ToString();
        if (defeatedEnemies != null) defeatedEnemies.text = kills.ToString();
        if (characterName != null) characterName.text = playerName;

        if (weaponRows != null)
        {
            for (int i = 0; i < weaponRows.Length; i++)
            {
                WeaponRow row = weaponRows[i];
                if (row == null) continue;
                Weapon weapon = weapons != null && i < weapons.Count ? weapons[i] : null;
                if (row.root != null) row.root.SetActive(weapon != null);
                if (weapon == null) continue;

                SetIcon(row.icon, weapon.Icon);
                if (row.weaponName != null) row.weaponName.text = weapon.DisplayName;
                if (row.level != null) row.level.text = $"Lv.{weapon.Level}";
                if (row.acquisitionTime != null) row.acquisitionTime.text = FormatTime(weapon.AcquisitionTime);
            }
        }

        if (accessorySlots != null)
        {
            for (int i = 0; i < accessorySlots.Length; i++)
            {
                AccessorySlot slot = accessorySlots[i];
                if (slot == null) continue;
                Accessory accessory = accessories != null && i < accessories.Count ? accessories[i] : null;
                if (slot.root != null) slot.root.SetActive(accessory != null);
                if (accessory == null) continue;

                SetIcon(slot.icon, accessory.Icon);
                if (slot.level != null) slot.level.text = $"Lv.{accessory.Level}";
            }
        }
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static string FormatTime(float elapsed)
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }

    public void Show()
    {
        if (panel == null) return;
        panel.SetActive(true);
        if (mainScreenButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(mainScreenButton.gameObject);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
