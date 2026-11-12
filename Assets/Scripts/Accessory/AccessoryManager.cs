using System.Collections.Generic;
using UnityEngine;

public class AccessoryManager : MonoBehaviour
{
    [SerializeField] private Accessory[] accessoryPrefabs;
    [SerializeField, Min(0)] private int maximumAccessories = 3;
    private readonly Dictionary<Accessory, Accessory> ownedAccessories = new();

    private readonly List<Accessory> acquisitionOrder = new();
    public IReadOnlyList<Accessory> OwnedAccessories => acquisitionOrder;
    public event System.Action OwnedItemsChanged;

    public int OwnedCount => ownedAccessories.Count;

    public Accessory GetOwnedAccessory(Accessory prefab)
    {
        if (prefab == null) return null;
        ownedAccessories.TryGetValue(prefab, out Accessory accessory);
        return accessory;
    }

    public void AddChoices(List<ILevelUpOption> choices)
    {
        if (accessoryPrefabs == null) return;
        for (int i = 0; i < accessoryPrefabs.Length; i++)
        {
            Accessory prefab = accessoryPrefabs[i];
            if (prefab == null) continue;
            Accessory owned = GetOwnedAccessory(prefab);
            if (owned == null && OwnedCount >= maximumAccessories) continue;
            Accessory choice = owned != null ? owned : prefab;
            if (choice.CanUpgrade && !choices.Contains(choice)) choices.Add(choice);
        }
    }

    public bool AcquireOrUpgrade(Accessory choice)
    {
        return AcquireOrUpgrade(choice, false);
    }

    public bool AcquireFromMap(Accessory prefab)
    {
        return AcquireOrUpgrade(prefab, true);
    }

    private bool AcquireOrUpgrade(Accessory choice, bool ignoreLimit)
    {
        if (choice == null || accessoryPrefabs == null) return false;
        for (int i = 0; i < accessoryPrefabs.Length; i++)
        {
            Accessory prefab = accessoryPrefabs[i];
            if (prefab == null) continue;
            Accessory owned = GetOwnedAccessory(prefab);
            if (choice != prefab && choice != owned) continue;
            if (owned != null)
            {
                if (!owned.CanUpgrade) return false;
                owned.LevelUp();
            }
            else
            {
                if (!ignoreLimit && OwnedCount >= maximumAccessories) return false;
                Accessory instance = Instantiate(prefab, transform);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.Initialize(transform);
                instance.Acquire();
                ownedAccessories[prefab] = instance;
                acquisitionOrder.Add(instance);
                OwnedItemsChanged?.Invoke();
            }
            return true;
        }
        return false;
    }
}
