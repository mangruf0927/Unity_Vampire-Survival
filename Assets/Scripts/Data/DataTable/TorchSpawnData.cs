using Newtonsoft.Json;
using UnityEngine;

public class TorchSpawnData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public float spawnInterval;
    [JsonProperty(Required = Required.Always)] public float spawnChance;
    [JsonProperty(Required = Required.Always)] public int maximumTorches;
    [JsonProperty(Required = Required.Always)] public float spawnMargin;
    [JsonProperty(Required = Required.Always)] public float minimumSpacing;
    [JsonProperty(Required = Required.Always)] public int placementAttempts;
    [JsonProperty(Required = Required.Always)] public float returnDistance;

    public bool Validate()
    {
        if (Id <= 0 || !(spawnInterval >= 0.1f) || !(spawnChance >= 0f && spawnChance <= 1f)
            || maximumTorches < 0 || !(spawnMargin >= 0f) || !(minimumSpacing >= 0f)
            || placementAttempts < 1 || !(returnDistance >= 1f))
        {
            Debug.LogError($"TorchSpawnData의 ID, 생성 간격/확률/개수 또는 거리 설정을 확인해 주세요. ID: {Id}");
            return false;
        }
        return true;
    }
}
