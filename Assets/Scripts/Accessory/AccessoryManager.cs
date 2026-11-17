using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AccessoryManager : MonoBehaviour
{
    [SerializeField] private Accessory[] accessoryPrefabs;
    [SerializeField, Min(0)] private int maximumAccessories = 3;

    private bool hasValidPrefabs;
    private readonly Dictionary<Accessory, Accessory> ownedAccessories = new();
    private readonly List<Accessory> acquisitionOrder = new();

    public IReadOnlyList<Accessory> OwnedAccessories => acquisitionOrder;
    public int OwnedCount => ownedAccessories.Count;
    public int MaximumAccessories => maximumAccessories;
    public event System.Action OwnedItemsChanged;

    private void Awake()
    {
        hasValidPrefabs = ValidatePrefabs();
    }

    private bool ValidatePrefabs()
    {
        if (accessoryPrefabs == null)
        {
            Debug.LogError("AccessoryManager: Accessory Prefabs 목록을 설정해 주세요.", this);
            return false;
        }

        bool valid = true;
        HashSet<int> registeredIds = new();
        for (int i = 0; i < accessoryPrefabs.Length; i++)
        {
            Accessory prefab = accessoryPrefabs[i];
            if (prefab == null)
            {
                Debug.LogError($"AccessoryManager: Accessory Prefabs[{i}]가 비어 있습니다.", this);
                valid = false;
                continue;
            }
            if (!registeredIds.Add(prefab.DataId))
            {
                Debug.LogError($"AccessoryManager: 장신구 Data ID {prefab.DataId}가 중복 등록되었습니다. 프리팹: {prefab.name}", this);
                valid = false;
            }
            if (!DataManager.Instance.Accessories.TryGet(prefab.DataId, out _))
            {
                Debug.LogError($"AccessoryManager: 프리팹 {prefab.name}의 Data ID {prefab.DataId}가 Accessories 테이블에 없습니다.", this);
                valid = false;
            }
        }
        return valid;
    }

    public Accessory GetOwnedAccessory(Accessory prefab)
    {
        if (prefab == null) return null;
        ownedAccessories.TryGetValue(prefab, out Accessory accessory);
        return accessory;
    }

    public void AddChoices(List<ILevelUpOption> choices)
    {
        if (!hasValidPrefabs) return;

        foreach (Accessory prefab in accessoryPrefabs)
        {
            if (prefab == null) continue;

            Accessory owned = GetOwnedAccessory(prefab);
            if (owned == null && OwnedCount >= maximumAccessories) continue;

            Accessory choice = owned != null ? owned : prefab;
            if (!choice.CanUpgrade || choices.Contains(choice)) continue;

            choices.Add(choice);
        }
    }

    public bool AcquireOrUpgrade(Accessory choice, bool ignoreLimit = false)
    {
        if (!hasValidPrefabs || choice == null) return false;

        foreach (Accessory prefab in accessoryPrefabs)
        {
            if (prefab == null) continue;

            Accessory owned = GetOwnedAccessory(prefab);
            if (choice != prefab && choice != owned) continue;

            if (owned != null) return UpgradeAccessory(owned);

            return AcquireAccessory(prefab, ignoreLimit);
        }

        return false;
    }

    private bool UpgradeAccessory(Accessory accessory)
    {
        if (!accessory.CanUpgrade) return false;
        accessory.LevelUp();
        return true;
    }

    private bool AcquireAccessory(Accessory prefab, bool ignoreLimit)
    {
        if (!ignoreLimit && OwnedCount >= maximumAccessories) return false;

        Accessory instance = Instantiate(prefab, transform);
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, quaternion.identity);
        instance.Initialize(transform);
        instance.Acquire();

        ownedAccessories[prefab] = instance;
        acquisitionOrder.Add(instance);

        OwnedItemsChanged?.Invoke();
        return true;
    }
}
