using System;
using Newtonsoft.Json;
using UnityEngine;

public class WeaponData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public string displayName;
    [JsonProperty(Required = Required.Always)] public string acquisitionDescription;
    [JsonProperty(Required = Required.Always)] public float damage;
    [JsonProperty(Required = Required.Always)] public int projectileCount;
    [JsonProperty(Required = Required.Always)] public float knockback;
    [JsonProperty(Required = Required.Always)] public float cooldown;

    public float burstInterval = 0.1f;
    public float projectileSpeed = 12f;
    public int penetrationCount = 1;
    public float speedMultiplier = 1f;
    public float areaMultiplier = 1f;
    public float duration = 3f;
    public float orbitRadius = 2f;
    public float rotationSpeed = 180f;
    public float hitboxDelay = 1.7f;
    public float slashLength = 3f;
    public float slashWidth = 0.7f;
    public float slashDuration = 0.15f;
    [JsonProperty(Required = Required.Always)] public string speedLabel;
    [JsonProperty(Required = Required.Always)] public WeaponUpgradeData[] upgrades;

    public bool Validate()
    {
        if (Id <= 0) return Invalid($"ID가 유효하지 않습니다. ID: {Id}");
        if (upgrades == null || upgrades.Length != 7)
            return Invalid("upgrades는 2~8레벨 순서의 7개 항목이어야 합니다.");

        for (int i = 0; i < upgrades.Length; i++)
        {
            if (upgrades[i] == null) return Invalid($"upgrades[{i}]가 null입니다.");
            if (upgrades[i].level != i + 2)
                return Invalid($"upgrades[{i}].level은 {i + 2}여야 합니다.");
        }
        return true;
    }

    public WeaponUpgradeData GetUpgrade(int level)
    {
        return level >= 2 && level <= 8 ? upgrades[level - 2] : null;
    }

    private bool Invalid(string message)
    {
        Debug.LogError($"WeaponData ID {Id}: {message}");
        return false;
    }
}
