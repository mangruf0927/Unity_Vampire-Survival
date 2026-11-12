using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AccessoryManager : MonoBehaviour
{
    [SerializeField] private Accessory[] accessoryPrefabs;
    [SerializeField, Min(0)] private int maximumAccessories = 3;

    private readonly Dictionary<Accessory, Accessory> ownedAccessories = new();
    private readonly List<Accessory> acquisitionOrder = new();

    public IReadOnlyList<Accessory> OwnedAccessories => acquisitionOrder;
    public int OwnedCount => ownedAccessories.Count;
    public event System.Action OwnedItemsChanged;

    public Accessory GetOwnedAccessory(Accessory prefab)
    {
        if (prefab == null) return null;
        ownedAccessories.TryGetValue(prefab, out Accessory accessory);
        return accessory;
    }

    public void AddChoices(List<ILevelUpOption> choices)
    {
        if (accessoryPrefabs == null) return;

        foreach (Accessory prefab in accessoryPrefabs)
        {
            if (prefab == null) return;

            Accessory owned = GetOwnedAccessory(prefab);
            if (owned == null && OwnedCount >= maximumAccessories) continue;

            Accessory choice = owned != null ? owned : prefab;
            if (!choice.CanUpgrade || choices.Contains(choice)) continue;

            choices.Add(choice);
        }
    }

    public bool AcquireOrUpgrade(Accessory choice, bool ignoreLimit = false)
    {
        if (choice == null || accessoryPrefabs == null) return false;

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
