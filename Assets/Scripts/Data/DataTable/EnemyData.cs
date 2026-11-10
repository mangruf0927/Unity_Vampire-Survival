using System;
using Newtonsoft.Json;
using UnityEngine;

public class EnemyData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public float maxHealth;
    [JsonProperty(Required = Required.Always)] public float moveSpeed;
    [JsonProperty(Required = Required.Always)] public float rotationSpeed;
    [JsonProperty(Required = Required.Always)] public float damage;
    [JsonProperty(Required = Required.Always)] public float gemDropChance;
    [JsonProperty(Required = Required.Always)] public float offscreenReturnDelay;
    [JsonProperty(Required = Required.Always)] public float knockbackDistance;
    [JsonProperty(Required = Required.Always)] public float knockbackDuration;

    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"EnemyData ID가 유효하지 않습니다. ID: {Id}");
            return false;
        }
        return true;
    }
}
