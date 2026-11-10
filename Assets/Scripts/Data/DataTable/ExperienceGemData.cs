using System;
using Newtonsoft.Json;
using UnityEngine;

public class ExperienceGemData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public int experienceValue;

    public bool Validate()
    {
        if (Id <= 0)
        {
            Debug.LogError($"ExperienceGemData ID가 유효하지 않습니다. ID: {Id}");
            return false;
        }
        return true;
    }
}
