using Newtonsoft.Json;
using UnityEngine;

public class AccessoryData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public float bonusPerLevel;

    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"Accessory Id is invalid. Id: {Id}");
            return false;
        }
        return true;
    }
}
