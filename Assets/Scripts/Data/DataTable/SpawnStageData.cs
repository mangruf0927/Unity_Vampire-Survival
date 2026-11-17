using Newtonsoft.Json;
using UnityEngine;

public class SpawnStageData : IValidatable
{
    [JsonProperty(Required = Required.Always)] public float startTime;
    [JsonProperty(Required = Required.Always)] public EnemyEntryData[] enemies;
    [JsonProperty(Required = Required.Always)] public float spawnInterval;
    [JsonProperty(Required = Required.Always)] public int minimumEnemies;
    [JsonProperty(Required = Required.Always)] public int maximumEnemies;

    public bool Validate()
    {
        if (float.IsNaN(startTime) || float.IsInfinity(startTime) || startTime < 0f
            || enemies == null || enemies.Length == 0
            || float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval) || spawnInterval <= 0f
            || minimumEnemies < 0 || maximumEnemies < 0)
        {
            Debug.LogError($"SpawnStageData의 생성 설정을 확인해 주세요. 시작 시각: {startTime}");
            return false;
        }
        bool hasPositiveWeight = false;
        foreach (EnemyEntryData entry in enemies)
        {
            if (entry == null || !entry.Validate()) return false;
            if (entry.weight > 0f) hasPositiveWeight = true;
        }
        if (!hasPositiveWeight) Debug.LogError($"SpawnStageData에 양수 가중치가 필요합니다. 시작 시각: {startTime}");
        return hasPositiveWeight;
    }
}
