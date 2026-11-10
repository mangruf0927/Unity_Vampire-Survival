using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class WeaponUpgradeData
{
    [JsonProperty(Required = Required.Always)] public int level;
    public float damage;
    public int projectileCount;
    public float cooldown;
    public float burstInterval;
    public int penetrationCount;
    public float duration;
    public float speedMultiplier;
    public float areaMultiplier;

    public bool Validate()
    {
        if (level < 2 || level > 8) return Invalid("level은 2~8이어야 합니다.");
        return true;
    }

    public string GetDescription(string speedLabel)
    {
        List<string> effects = new();
        AddEffect(effects, "피해량이", damage, "");
        AddEffect(effects, "투사체 수가", projectileCount, "개");
        AddEffect(effects, "쿨타임이", cooldown, "초");
        AddEffect(effects, "연사 간격이", burstInterval, "초");
        AddEffect(effects, "관통 수가", penetrationCount, "");
        AddEffect(effects, "지속시간이", duration, "초");
        AddEffect(effects, speedLabel + "가", speedMultiplier * 100f, "%");
        AddEffect(effects, "공격 범위가", areaMultiplier * 100f, "%");
        return effects.Count > 0 ? string.Join("\n", effects) : "능력치 변화 없음";
    }

    private static void AddEffect(List<string> effects, string name, float value, string unit)
    {
        if (value == 0f) return;
        string change = value > 0f ? " 증가합니다." : " 감소합니다.";
        effects.Add(name + " " + Math.Abs(value).ToString("0.##") + unit + change);
    }

    private bool Invalid(string message)
    {
        Debug.LogError($"WeaponUpgradeData level {level}: {message}");
        return false;
    }
}
