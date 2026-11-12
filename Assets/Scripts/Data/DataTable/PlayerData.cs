using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public enum CharacterAbility { DamagePerLevel, ProjectileSpeedPerLevel, ExtraProjectile }

public class PlayerData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public string displayName;
    [JsonProperty(Required = Required.Always)] public int weaponId;
    [JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))] public CharacterAbility ability;
    [JsonProperty(Required = Required.Always)] public string abilityDescription;
    [JsonProperty(Required = Required.Always)] public int abilityInterval;
    [JsonProperty(Required = Required.Always)] public float abilityBonus;
    [JsonProperty(Required = Required.Always)] public float maxAbilityBonus;
    [JsonProperty(Required = Required.Always)] public float maxHealth;
    [JsonProperty(Required = Required.Always)] public float damageInterval;
    [JsonProperty(Required = Required.Always)] public float moveSpeed;
    [JsonProperty(Required = Required.Always)] public float rotationSpeed;
    [JsonProperty(Required = Required.Always)] public int initialExp;
    [JsonProperty(Required = Required.Always)] public int expGrowth;
    [JsonProperty(Required = Required.Always)] public float attractionRange;
    [JsonProperty(Required = Required.Always)] public float attractionSpeed;

    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"PlayerData ID가 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogError($"PlayerData displayName이 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (weaponId <= 0)
        {
            Debug.LogError($"PlayerData weaponId가 유효하지 않습니다. ID: {Id}, weaponId: {weaponId}");
            return false;
        }

        if (!Enum.IsDefined(typeof(CharacterAbility), ability))
        {
            Debug.LogError($"PlayerData ability가 유효하지 않습니다. ID: {Id}, ability: {ability}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(abilityDescription))
        {
            Debug.LogError($"PlayerData abilityDescription이 유효하지 않습니다. ID: {Id}");
            return false;
        }

        if (abilityInterval < 0 || (ability != CharacterAbility.ExtraProjectile && abilityInterval == 0))
        {
            Debug.LogError($"PlayerData abilityInterval이 유효하지 않습니다. ID: {Id}, abilityInterval: {abilityInterval}");
            return false;
        }

        if (float.IsNaN(abilityBonus) || float.IsInfinity(abilityBonus) || abilityBonus < 0)
        {
            Debug.LogError($"PlayerData abilityBonus가 유효하지 않습니다. ID: {Id}, abilityBonus: {abilityBonus}");
            return false;
        }

        if (float.IsNaN(maxAbilityBonus) || float.IsInfinity(maxAbilityBonus) || maxAbilityBonus < 0)
        {
            Debug.LogError($"PlayerData maxAbilityBonus가 유효하지 않습니다. ID: {Id}, maxAbilityBonus: {maxAbilityBonus}");
            return false;
        }

        if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth <= 0)
        {
            Debug.LogError($"PlayerData maxHealth가 유효하지 않습니다. ID: {Id}, maxHealth: {maxHealth}");
            return false;
        }

        if (float.IsNaN(damageInterval) || float.IsInfinity(damageInterval) || damageInterval <= 0)
        {
            Debug.LogError($"PlayerData damageInterval이 유효하지 않습니다. ID: {Id}, damageInterval: {damageInterval}");
            return false;
        }

        if (float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed) || moveSpeed < 0)
        {
            Debug.LogError($"PlayerData moveSpeed가 유효하지 않습니다. ID: {Id}, moveSpeed: {moveSpeed}");
            return false;
        }

        if (float.IsNaN(rotationSpeed) || float.IsInfinity(rotationSpeed) || rotationSpeed < 0)
        {
            Debug.LogError($"PlayerData rotationSpeed가 유효하지 않습니다. ID: {Id}, rotationSpeed: {rotationSpeed}");
            return false;
        }

        if (initialExp <= 0)
        {
            Debug.LogError($"PlayerData initialExp가 유효하지 않습니다. ID: {Id}, initialExp: {initialExp}");
            return false;
        }

        if (expGrowth < 0)
        {
            Debug.LogError($"PlayerData expGrowth가 유효하지 않습니다. ID: {Id}, expGrowth: {expGrowth}");
            return false;
        }

        if (float.IsNaN(attractionRange) || float.IsInfinity(attractionRange) || attractionRange < 0)
        {
            Debug.LogError($"PlayerData attractionRange가 유효하지 않습니다. ID: {Id}, attractionRange: {attractionRange}");
            return false;
        }

        if (float.IsNaN(attractionSpeed) || float.IsInfinity(attractionSpeed) || attractionSpeed < 0)
        {
            Debug.LogError($"PlayerData attractionSpeed가 유효하지 않습니다. ID: {Id}, attractionSpeed: {attractionSpeed}");
            return false;
        }

        return true;
    }
}
