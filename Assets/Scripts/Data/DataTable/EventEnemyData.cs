using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public enum EventEnemyDirection { ToPlayer, Fixed }

// 체력/피해는 프리팹이 참조하는 EnemyData, 이벤트 생성과 이동 설정은 StageData 내부의 이벤트 설정이 담당한다.
public class EventEnemyData : IGameData, IValidatable
{
    [JsonProperty(Required = Required.Always)] public int Id { get; set; }
    [JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))] public PoolTypeEnums poolType;
    [JsonProperty(Required = Required.Always)] public float[] spawnTimes;
    [JsonProperty(Required = Required.Always)] public int count;
    [JsonProperty(Required = Required.Always)] public int maxActive;
    [JsonProperty(Required = Required.Always)] public float moveSpeed;
    [JsonProperty(Required = Required.Always)] public float spacing;
    [JsonProperty(Required = Required.Always)] public float pushStrength;
    [JsonProperty(Required = Required.Always)] public float maxOffset;
    [JsonProperty(Required = Required.Always)] public float recoverySpeed;
    [JsonProperty(Required = Required.Always), JsonConverter(typeof(StringEnumConverter))] public EventEnemyDirection directionMode;
    [JsonProperty(Required = Required.DisallowNull)] public float? directionX;
    [JsonProperty(Required = Required.DisallowNull)] public float? directionZ;

    public bool Validate()
    {
        if (Id <= 0 || !Enum.IsDefined(typeof(PoolTypeEnums), poolType)
            || !Enum.IsDefined(typeof(EventEnemyDirection), directionMode)
            || spawnTimes == null || spawnTimes.Length == 0)
        {
            Debug.LogError($"EventEnemyData의 ID, 종류, 방향 또는 생성 시각을 확인해 주세요. ID: {Id}");
            return false;
        }
        for (int i = 0; i < spawnTimes.Length; i++)
        {
            if (!IsNonNegative(spawnTimes[i]) || (i > 0 && spawnTimes[i] <= spawnTimes[i - 1]))
            {
                Debug.LogError($"EventEnemyData 생성 시각은 0 이상 오름차순이어야 합니다. ID: {Id}");
                return false;
            }
        }
        if (count < 1 || maxActive < 0 || !IsNonNegative(moveSpeed) || moveSpeed <= 0f
            || !IsNonNegative(spacing) || spacing < 1.1f || !IsNonNegative(pushStrength) || pushStrength < 1f
            || !IsNonNegative(maxOffset) || !IsNonNegative(recoverySpeed))
        {
            Debug.LogError($"EventEnemyData의 생성/이동 설정을 확인해 주세요. ID: {Id}");
            return false;
        }
        if (directionMode == EventEnemyDirection.Fixed
            && (!directionX.HasValue || !directionZ.HasValue
                || float.IsNaN(directionX.Value) || float.IsInfinity(directionX.Value)
                || float.IsNaN(directionZ.Value) || float.IsInfinity(directionZ.Value)
                || new Vector3(directionX.Value, 0f, directionZ.Value).sqrMagnitude <= 0.000001f))
        {
            Debug.LogError($"EventEnemyData 고정 방향에는 유효한 directionX/Z가 필요합니다. ID: {Id}");
            return false;
        }
        return true;
    }

    public Vector3 GetDirection(Vector3 start, Vector3 player)
    {
        Vector3 direction = directionMode == EventEnemyDirection.ToPlayer
            ? player - start : new Vector3(directionX.Value, 0f, directionZ.Value);
        direction.y = 0f;
        return direction.normalized;
    }

    private static bool IsNonNegative(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
