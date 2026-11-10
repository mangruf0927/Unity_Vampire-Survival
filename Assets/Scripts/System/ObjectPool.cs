using System.Collections.Generic;
using UnityEngine;

public enum PoolTypeEnums { Bat = 0, ExperienceGem = 1, Zombie = 2, Skeleton = 3, Ghost = 4, Mudman = 5, ForestChunk = 6 }

[System.Serializable]
public class PoolData
{
    public PoolTypeEnums poolType;
    public GameObject prefab;
    [Min(0)] public int size = 10;
    public Transform parent;
}

public class ObjectPool
{
    private readonly Dictionary<PoolTypeEnums, Queue<GameObject>> poolDictionary = new();
    private readonly Dictionary<PoolTypeEnums, PoolData> dataDictionary = new();
    private readonly Dictionary<PoolTypeEnums, Transform> parentDictionary = new();

    public static ObjectPool Instance { get; } = new();

    private readonly Dictionary<GameObject, PoolTypeEnums> objectPoolTypes = new();
    private readonly HashSet<GameObject> pooledObjects = new();

    private Transform inactiveRoot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        Instance.poolDictionary.Clear();
        Instance.dataDictionary.Clear();
        Instance.parentDictionary.Clear();
        Instance.objectPoolTypes.Clear();
        Instance.pooledObjects.Clear();
        Instance.inactiveRoot = null;
    }

    public bool TryGetPrefab(PoolTypeEnums poolType, out GameObject prefab)
    {
        prefab = null;
        if (!dataDictionary.TryGetValue(poolType, out PoolData data)) return false;
        prefab = data.prefab;
        return prefab != null;
    }

    public void Unregister(PoolTypeEnums poolType)
    {
        List<GameObject> ownedObjects = new();
        foreach (KeyValuePair<GameObject, PoolTypeEnums> pair in objectPoolTypes)
        {
            if (pair.Value == poolType) ownedObjects.Add(pair.Key);
        }
        for (int i = 0; i < ownedObjects.Count; i++) RemoveObject(ownedObjects[i]);

        poolDictionary.Remove(poolType);
        dataDictionary.Remove(poolType);
        parentDictionary.Remove(poolType);
    }

    public void Register(PoolData data, Transform parent)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError($"{data?.poolType} pool prefab is missing.");
            return;
        }

        if (dataDictionary.ContainsKey(data.poolType)) return;

        dataDictionary.Add(data.poolType, data);
        poolDictionary.Add(data.poolType, new Queue<GameObject>());

        Transform poolParent = data.parent != null ? data.parent : parent;
        parentDictionary.Add(data.poolType, poolParent);

        InitializePool(data.size, data.prefab, data.poolType, poolParent);
    }

    private void InitializePool(int poolSize, GameObject prefab, PoolTypeEnums poolType, Transform parent = null)
    {
        Queue<GameObject> pool = poolDictionary[poolType];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = CreatePool(prefab, poolType, parent);
            pooledObjects.Add(obj);
            pool.Enqueue(obj);
        }
    }

    public GameObject GetFromPool(PoolTypeEnums poolType)
    {
        GameObject obj = TakeFromPool(poolType);
        if (obj == null) return null;

        obj.SetActive(true);
        return obj;
    }

    public GameObject GetFromPool(PoolTypeEnums poolType, Vector3 position, Quaternion rotation)
    {
        GameObject obj = TakeFromPool(poolType);
        if (obj == null) return null;

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    private GameObject TakeFromPool(PoolTypeEnums poolType)
    {
        if (!poolDictionary.TryGetValue(poolType, out Queue<GameObject> pool)) return null;
        if (!dataDictionary.TryGetValue(poolType, out PoolData data)) return null;

        GameObject obj = null;
        while (pool.Count > 0 && obj == null)
        {
            obj = pool.Dequeue();
            if (obj == null && !object.ReferenceEquals(obj, null))
            {
                objectPoolTypes.Remove(obj);
                pooledObjects.Remove(obj);
            }
        }
        if (obj == null) obj = CreatePool(data.prefab, poolType, parentDictionary[poolType]);

        pooledObjects.Remove(obj);
        obj.transform.SetParent(parentDictionary[poolType], false);
        return obj;
    }

    private GameObject CreatePool(GameObject prefab, PoolTypeEnums poolType, Transform parent = null)
    {
        GameObject obj = Object.Instantiate(prefab, GetInactiveRoot());
        objectPoolTypes.Add(obj, poolType);
        obj.SetActive(false);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private Transform GetInactiveRoot()
    {
        if (inactiveRoot != null) return inactiveRoot;

        GameObject root = new("InactiveRoot");
        root.SetActive(false);
        Object.DontDestroyOnLoad(root);

        inactiveRoot = root.transform;
        return inactiveRoot;
    }

    public bool TryReturnToPool(GameObject obj)
    {
        if (obj == null || !objectPoolTypes.TryGetValue(obj, out PoolTypeEnums type))
        {
            RemoveObject(obj);
            return false;
        }
        ReturnToPool(obj, type);
        return true;
    }

    public void ReturnToPool(GameObject obj, PoolTypeEnums poolType)
    {
        if (obj == null)
        {
            RemoveObject(obj);
            return;
        }
        if (pooledObjects.Contains(obj)) return;

        if (!objectPoolTypes.TryGetValue(obj, out PoolTypeEnums originalType))
        {
            RemoveObject(obj);
            return;
        }
        if (originalType != poolType)
        {
            Debug.LogWarning($"ObjectPool: {originalType} 오브젝트를 {poolType} 풀로 반환할 수 없어 제거합니다.", obj);
            RemoveObject(obj);
            return;
        }
        if (!poolDictionary.TryGetValue(poolType, out Queue<GameObject> pool) || !TryGetPrefab(poolType, out _) || !parentDictionary.TryGetValue(poolType, out Transform parent))
        {
            RemoveObject(obj);
            return;
        }

        pooledObjects.Add(obj);
        obj.SetActive(false);
        obj.transform.SetParent(parent, false);
        pool.Enqueue(obj);
    }

    private void RemoveObject(GameObject obj)
    {
        if (ReferenceEquals(obj, null)) return;
        objectPoolTypes.Remove(obj);
        pooledObjects.Remove(obj);
        if (obj == null) return;

        obj.SetActive(false);
        Object.Destroy(obj);
    }
}
