using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public enum WeaponAttackType { Projectile, Orbit, Slash }

public class WeaponData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public string displayName;
    [JsonProperty(Required = Required.Always)] public string acquisitionDescription;
    [JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))] public WeaponAttackType attackType;
    [JsonProperty(Required = Required.Always)] public float damage;
    [JsonProperty(Required = Required.Always)] public int projectileCount;
    [JsonProperty(Required = Required.Always)] public float knockback;
    [JsonProperty(Required = Required.Always)] public float cooldown;

    [JsonProperty(Required = Required.DisallowNull)] public float? burstInterval;
    [JsonProperty(Required = Required.DisallowNull)] public float? projectileSpeed;
    [JsonProperty(Required = Required.DisallowNull)] public int? penetrationCount;

    [JsonProperty(Required = Required.DisallowNull)] public float? speedMultiplier;
    [JsonProperty(Required = Required.DisallowNull)] public float? areaMultiplier;

    [JsonProperty(Required = Required.DisallowNull)] public float? duration;
    [JsonProperty(Required = Required.DisallowNull)] public float? orbitRadius;
    [JsonProperty(Required = Required.DisallowNull)] public float? rotationSpeed;
    [JsonProperty(Required = Required.DisallowNull)] public float? hitboxDelay;

    [JsonProperty(Required = Required.DisallowNull)] public float? slashLength;
    [JsonProperty(Required = Required.DisallowNull)] public float? slashWidth;
    [JsonProperty(Required = Required.DisallowNull)] public float? slashDuration;
    [JsonProperty(Required = Required.Always)] public WeaponUpgradeData[] upgrades;


    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"WeaponData ID가 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogError($"WeaponData displayName이 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(acquisitionDescription))
        {
            Debug.LogError($"WeaponData acquisitionDescription이 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (!Enum.IsDefined(typeof(WeaponAttackType), attackType))
        {
            Debug.LogError($"WeaponData attackType이 유효하지 않습니다. ID: {Id}, attackType: {attackType}");
            return false;
        }

        if (!IsNonNegative(damage))
        {
            Debug.LogError($"WeaponData damage가 유효하지 않습니다. ID: {Id}, damage: {damage}");
            return false;
        }

        if (projectileCount < 1)
        {
            Debug.LogError($"WeaponData projectileCount가 유효하지 않습니다. ID: {Id}, projectileCount: {projectileCount}");
            return false;
        }

        if (!IsNonNegative(knockback))
        {
            Debug.LogError($"WeaponData knockback이 유효하지 않습니다. ID: {Id}, knockback: {knockback}");
            return false;
        }

        if (!IsPositive(cooldown))
        {
            Debug.LogError($"WeaponData cooldown이 유효하지 않습니다. ID: {Id}, cooldown: {cooldown}");
            return false;
        }

        if ((attackType == WeaponAttackType.Projectile || attackType == WeaponAttackType.Slash) && !IsPositive(burstInterval))
        {
            Debug.LogError($"WeaponData Projectile/Slash에는 양수 burstInterval이 필요합니다. ID: {Id}, burstInterval: {burstInterval}");
            return false;
        }

        if (attackType == WeaponAttackType.Projectile && (!IsPositive(projectileSpeed) || !penetrationCount.HasValue || penetrationCount.Value < 1))
        {
            Debug.LogError($"WeaponData Projectile 설정이 유효하지 않습니다. ID: {Id}, projectileSpeed: {projectileSpeed}, penetrationCount: {penetrationCount}");
            return false;
        }

        if (attackType == WeaponAttackType.Orbit && (!IsPositive(duration) || !IsPositive(orbitRadius) || !IsPositive(rotationSpeed) || !IsPositive(hitboxDelay)))
        {
            Debug.LogError($"WeaponData Orbit 설정이 유효하지 않습니다. ID: {Id}, duration: {duration}, orbitRadius: {orbitRadius}, rotationSpeed: {rotationSpeed}, hitboxDelay: {hitboxDelay}");
            return false;
        }

        if (attackType == WeaponAttackType.Slash && (!IsPositive(slashLength) || !IsPositive(slashWidth) || !IsPositive(slashDuration)))
        {
            Debug.LogError($"WeaponData Slash 설정이 유효하지 않습니다. ID: {Id}, slashLength: {slashLength}, slashWidth: {slashWidth}, slashDuration: {slashDuration}");
            return false;
        }

        if (!IsOptionalPositive(burstInterval) || !IsOptionalPositive(projectileSpeed)
            || !IsOptionalPositive(speedMultiplier) || !IsOptionalPositive(areaMultiplier)
            || !IsOptionalPositive(duration) || !IsOptionalPositive(orbitRadius)
            || !IsOptionalPositive(rotationSpeed) || !IsOptionalPositive(hitboxDelay)
            || !IsOptionalPositive(slashLength) || !IsOptionalPositive(slashWidth)
            || !IsOptionalPositive(slashDuration)
            || (penetrationCount.HasValue && penetrationCount.Value < 1))
        {
            Debug.LogError($"WeaponData 무기 전용 수치가 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (upgrades == null || upgrades.Length != 7)
        {
            Debug.LogError($"WeaponData upgrades는 2~8레벨의 7개 항목이어야 합니다. ID: {Id}");
            return false;
        }

        for (int i = 0; i < upgrades.Length; i++)
        {
            WeaponUpgradeData upgrade = upgrades[i];

            if (upgrade == null || upgrade.level != i + 2 || !upgrade.Validate())
            {
                Debug.LogError($"WeaponData upgrades[{i}]가 유효하지 않습니다. ID: {Id}, 예상 레벨: {i + 2}");
                return false;
            }

            if ((attackType == WeaponAttackType.Projectile && upgrade.duration != 0f)
                || (attackType == WeaponAttackType.Orbit && (upgrade.burstInterval != 0f || upgrade.penetrationCount != 0))
                || (attackType == WeaponAttackType.Slash && (upgrade.penetrationCount != 0 || upgrade.duration != 0f || upgrade.speedMultiplier != 0f)))
            {
                Debug.LogError($"WeaponData upgrades[{i}]에 사용하지 않는 강화 수치가 있습니다. ID: {Id}");
                return false;
            }
        }

        return true;
    }

    public WeaponUpgradeData GetUpgrade(int level)
    {
        return level >= 2 && level <= 8 ? upgrades[level - 2] : null;
    }

    private static bool IsOptionalPositive(float? value)
    {
        return !value.HasValue || IsPositive(value);
    }

    private static bool IsPositive(float? value)
    {
        return IsNonNegative(value) && value.Value > 0f;
    }

    private static bool IsNonNegative(float? value)
    {
        return value.HasValue && !float.IsNaN(value.Value) && !float.IsInfinity(value.Value) && value.Value >= 0f;
    }
}
