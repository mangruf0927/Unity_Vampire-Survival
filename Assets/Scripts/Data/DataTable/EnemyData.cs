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

        if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth <= 0)
        {
            Debug.LogError($"EnemyData maxHealth가 유효하지 않습니다. ID: {Id}, maxHealth: {maxHealth}");
            return false;
        }

        if (float.IsNaN(moveSpeed) || float.IsInfinity(moveSpeed) || moveSpeed < 0)
        {
            Debug.LogError($"EnemyData moveSpeed가 유효하지 않습니다. ID: {Id}, moveSpeed: {moveSpeed}");
            return false;
        }

        if (float.IsNaN(rotationSpeed) || float.IsInfinity(rotationSpeed) || rotationSpeed < 0)
        {
            Debug.LogError($"EnemyData rotationSpeed가 유효하지 않습니다. ID: {Id}, rotationSpeed: {rotationSpeed}");
            return false;
        }

        if (float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0)
        {
            Debug.LogError($"EnemyData damage가 유효하지 않습니다. ID: {Id}, damage: {damage}");
            return false;
        }

        if (float.IsNaN(gemDropChance) || float.IsInfinity(gemDropChance) || gemDropChance < 0 || gemDropChance > 1)
        {
            Debug.LogError($"EnemyData gemDropChance가 유효하지 않습니다. ID: {Id}, gemDropChance: {gemDropChance}");
            return false;
        }

        if (float.IsNaN(offscreenReturnDelay) || float.IsInfinity(offscreenReturnDelay) || offscreenReturnDelay < 0)
        {
            Debug.LogError($"EnemyData offscreenReturnDelay가 유효하지 않습니다. ID: {Id}, offscreenReturnDelay: {offscreenReturnDelay}");
            return false;
        }

        if (float.IsNaN(knockbackDistance) || float.IsInfinity(knockbackDistance) || knockbackDistance < 0)
        {
            Debug.LogError($"EnemyData knockbackDistance가 유효하지 않습니다. ID: {Id}, knockbackDistance: {knockbackDistance}");
            return false;
        }

        if (float.IsNaN(knockbackDuration) || float.IsInfinity(knockbackDuration) || knockbackDuration < 0)
        {
            Debug.LogError($"EnemyData knockbackDuration가 유효하지 않습니다. ID: {Id}, knockbackDuration: {knockbackDuration}");
            return false;
        }

        return true;
    }
}
