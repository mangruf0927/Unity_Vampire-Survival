using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public enum CharacterAbility { DamagePerLevel, ProjectileSpeedPerLevel, ExtraProjectile }

public class PlayerData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public string displayName;
    [JsonProperty(Required = Required.Always)] public int startingWeaponId;
    [JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))] public CharacterAbility ability;
    [JsonProperty(Required = Required.Always)] public string abilityDescription;
    [JsonProperty(Required = Required.Always)] public int abilityLevelInterval;
    [JsonProperty(Required = Required.Always)] public float abilityBonusPerStep;
    [JsonProperty(Required = Required.Always)] public float maximumAbilityBonus;
    [JsonProperty(Required = Required.Always)] public float maxHealth;
    [JsonProperty(Required = Required.Always)] public float damageInterval;
    [JsonProperty(Required = Required.Always)] public float moveSpeed;
    [JsonProperty(Required = Required.Always)] public float rotationSpeed;
    [JsonProperty(Required = Required.Always)] public int firstLevelRequirement;
    [JsonProperty(Required = Required.Always)] public int experienceIncreasePerLevel;
    [JsonProperty(Required = Required.Always)] public float attractionRange;
    [JsonProperty(Required = Required.Always)] public float attractionSpeed;

    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"PlayerData ID가 유효하지 않습니다. ID: {Id}");
            return false;
        }
        return true;
    }
}
