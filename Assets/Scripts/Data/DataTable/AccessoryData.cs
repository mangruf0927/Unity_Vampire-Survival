using Newtonsoft.Json;
using UnityEngine;

public class AccessoryData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public string displayName;
    [JsonProperty(Required = Required.Always)] public float bonusPerLevel;
    [JsonProperty(Required = Required.Always)] public string choiceDescription;

    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"Accessory Id is invalid. Id: {Id}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogError($"Accessory displayName is invalid. Id: {Id}");
            return false;
        }

        if (float.IsNaN(bonusPerLevel) || float.IsInfinity(bonusPerLevel) || bonusPerLevel <= 0)
        {
            Debug.LogError($"Accessory bonusPerLevel is invalid. Id: {Id}, bonusPerLevel: {bonusPerLevel}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(choiceDescription))
        {
            Debug.LogError($"Accessory choiceDescription is invalid. Id: {Id}");
            return false;
        }

        return true;
    }
}
