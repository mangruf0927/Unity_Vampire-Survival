using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Weapon startingWeapon;
    [SerializeField] private Weapon[] weaponPrefabs;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private Camera gameCamera;

    private readonly Dictionary<Weapon, Weapon> ownedWeapons = new();

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (attackOrigin == null) attackOrigin = transform;
        if (gameCamera == null) gameCamera = Camera.main;
        if (startingWeapon != null) AcquireOrUpgrade(startingWeapon);
    }

    private Vector3 GetAttackDirection()
    {
        return playerMovement != null ? playerMovement.LastMoveDirection : attackOrigin.forward;
    }

    private void Update()
    {
        Vector3 direction = GetAttackDirection();
        foreach (Weapon weapon in ownedWeapons.Values)
            if (weapon != null) weapon.SetAttackDirection(direction);
    }

    public Weapon GetOwnedWeapon(Weapon prefab)
    {
        if (prefab == null) return null;
        ownedWeapons.TryGetValue(prefab, out Weapon weapon);
        return weapon;
    }

    public void FillChoices(List<Weapon> choices, int maximumCount)
    {
        choices.Clear();
        if (weaponPrefabs == null || maximumCount <= 0) return;
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            Weapon prefab = weaponPrefabs[i];
            if (prefab == null) continue;
            Weapon owned = GetOwnedWeapon(prefab);
            Weapon choice = owned != null ? owned : prefab;
            if (choice.CanUpgrade && !choices.Contains(choice)) choices.Add(choice);
        }

        if (choices.Count <= maximumCount) return;

        for (int i = 0; i < maximumCount; i++)
        {
            int index = Random.Range(i, choices.Count);
            Weapon swap = choices[i];
            choices[i] = choices[index];
            choices[index] = swap;
        }
        choices.RemoveRange(maximumCount, choices.Count - maximumCount);
    }

    public bool AcquireOrUpgrade(Weapon choice)
    {
        if (choice == null || weaponPrefabs == null) return false;
        for (int i = 0; i < weaponPrefabs.Length; i++)
        {
            Weapon prefab = weaponPrefabs[i];
            if (prefab == null) continue;
            Weapon owned = GetOwnedWeapon(prefab);
            if (choice != prefab && choice != owned) continue;

            if (owned != null)
            {
                if (!owned.CanUpgrade) return false;
                owned.LevelUp();
            }
            else
            {
                Weapon instance = Instantiate(prefab, transform);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.Initialize(attackOrigin, gameCamera, GetAttackDirection());
                instance.Acquire();
                ownedWeapons[prefab] = instance;
            }
            return true;
        }
        return false;
    }
}
