using System.Collections.Generic;
using UnityEngine;


public class PoolDataInitializer : MonoBehaviour
{
    [SerializeField] private PoolData[] pools = new PoolData[0];

    private readonly List<PoolTypeEnums> registeredPools = new();

    private void Awake()
    {
        for (int i = 0; i < pools.Length; i++)
        {
            PoolData data = pools[i];
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning("PoolDataInitializer의 풀 프리팹을 확인해 주세요.", this);
                continue;
            }
            if (registeredPools.Contains(data.poolType) || ObjectPool.Instance.TryGetPrefab(data.poolType, out _))
            {
                Debug.LogWarning($"{data.poolType} 풀은 이미 등록되어 있어 건너뜁니다.", this);
                continue;
            }

            ObjectPool.Instance.Register(data, transform);
            registeredPools.Add(data.poolType);
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < registeredPools.Count; i++)
        {
            ObjectPool.Instance.Unregister(registeredPools[i]);
        }
        registeredPools.Clear();
    }
}
