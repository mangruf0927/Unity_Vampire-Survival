using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public class EnemyEntryData : IValidatable
{
    [JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))] public PoolTypeEnums poolType;
    [JsonProperty(Required = Required.Always)] public float weight;

    public bool Validate()
    {
        if (!Enum.IsDefined(typeof(PoolTypeEnums), poolType)
            || float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
        {
            Debug.LogError($"EnemyEntryData의 풀 종류 또는 가중치를 확인해 주세요. 종류: {poolType}, 가중치: {weight}");
            return false;
        }
        return true;
    }
}
