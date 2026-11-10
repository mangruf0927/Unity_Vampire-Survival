using System;
using Newtonsoft.Json;
using UnityEngine;

public class PlayerData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
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
