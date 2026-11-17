using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class StageData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always)] public float duration;
    [JsonProperty(Required = Required.Always)] public SpawnStageData[] spawnStages;
    [JsonProperty(Required = Required.Always)] public EventEnemyData[] eventEnemies;

    public bool Validate()
    {
        if (Id <= 0 || float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f
            || spawnStages == null || spawnStages.Length == 0 || eventEnemies == null)
        {
            Debug.LogError($"StageData의 ID, 종료 시간 또는 생성 목록을 확인해 주세요. ID: {Id}");
            return false;
        }
        for (int i = 0; i < spawnStages.Length; i++)
        {
            SpawnStageData stage = spawnStages[i];
            if (stage == null || !stage.Validate() || stage.startTime >= duration
                || (i == 0 && stage.startTime != 0f)
                || (i > 0 && stage.startTime <= spawnStages[i - 1].startTime))
            {
                Debug.LogError($"StageData 일반 적 구간은 0초부터 종료 전까지 오름차순이어야 합니다. ID: {Id}, 구간: {i}");
                return false;
            }
        }
        HashSet<int> eventIds = new();
        foreach (EventEnemyData data in eventEnemies)
        {
            if (data == null || !data.Validate() || !eventIds.Add(data.Id))
            {
                Debug.LogError($"StageData 이벤트 설정 또는 중복 ID를 확인해 주세요. 스테이지 ID: {Id}, 이벤트 ID: {data?.Id}");
                return false;
            }
            foreach (float time in data.spawnTimes)
            {
                if (time >= duration)
                {
                    Debug.LogError($"StageData 이벤트는 종료 시간 전이어야 합니다. 스테이지 ID: {Id}, 이벤트 ID: {data.Id}");
                    return false;
                }
            }
        }
        return true;
    }
}
