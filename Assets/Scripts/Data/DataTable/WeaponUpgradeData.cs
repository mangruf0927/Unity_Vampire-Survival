using System;
using Newtonsoft.Json;
using UnityEngine;

public class WeaponUpgradeData
{
    [JsonProperty(Required = Required.Always)] public int level;
    [JsonProperty(Required = Required.Always)] public string description;

    [JsonProperty(Required = Required.DisallowNull)] public float damage;
    [JsonProperty(Required = Required.DisallowNull)] public int projectileCount;
    [JsonProperty(Required = Required.DisallowNull)] public float cooldown;
    [JsonProperty(Required = Required.DisallowNull)] public float burstInterval;
    [JsonProperty(Required = Required.DisallowNull)] public int penetrationCount;
    [JsonProperty(Required = Required.DisallowNull)] public float duration;
    [JsonProperty(Required = Required.DisallowNull)] public float speedMultiplier;
    [JsonProperty(Required = Required.DisallowNull)] public float areaMultiplier;

    public bool Validate()
    {
        if (level < 2 || level > 8)
        {
            Debug.LogError($"WeaponUpgradeData level이 유효하지 않습니다. level: {level}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            Debug.LogError($"WeaponUpgradeData description이 유효하지 않습니다. level: {level}");
            return false;
        }

        if (float.IsNaN(damage) || float.IsInfinity(damage))
        {
            Debug.LogError($"WeaponUpgradeData damage가 유효하지 않습니다. level: {level}, damage: {damage}");
            return false;
        }

        if (float.IsNaN(cooldown) || float.IsInfinity(cooldown))
        {
            Debug.LogError($"WeaponUpgradeData cooldown이 유효하지 않습니다. level: {level}, cooldown: {cooldown}");
            return false;
        }

        if (float.IsNaN(burstInterval) || float.IsInfinity(burstInterval))
        {
            Debug.LogError($"WeaponUpgradeData burstInterval이 유효하지 않습니다. level: {level}, burstInterval: {burstInterval}");
            return false;
        }

        if (float.IsNaN(duration) || float.IsInfinity(duration))
        {
            Debug.LogError($"WeaponUpgradeData duration이 유효하지 않습니다. level: {level}, duration: {duration}");
            return false;
        }

        if (float.IsNaN(speedMultiplier) || float.IsInfinity(speedMultiplier))
        {
            Debug.LogError($"WeaponUpgradeData speedMultiplier가 유효하지 않습니다. level: {level}, speedMultiplier: {speedMultiplier}");
            return false;
        }

        if (float.IsNaN(areaMultiplier) || float.IsInfinity(areaMultiplier))
        {
            Debug.LogError($"WeaponUpgradeData areaMultiplier가 유효하지 않습니다. level: {level}, areaMultiplier: {areaMultiplier}");
            return false;
        }

        return true;
    }

    public string GetDescription()
    {
        return description
            .Replace("{damage}", Format(damage))
            .Replace("{projectileCount}", Format(projectileCount))
            .Replace("{cooldown}", Format(cooldown))
            .Replace("{burstInterval}", Format(burstInterval))
            .Replace("{penetrationCount}", Format(penetrationCount))
            .Replace("{duration}", Format(duration))
            .Replace("{speedPercent}", Format(speedMultiplier * 100f))
            .Replace("{areaPercent}", Format(areaMultiplier * 100f));
    }

    private static string Format(float value)
    {
        return Math.Abs(value).ToString("0.##");
    }
}
