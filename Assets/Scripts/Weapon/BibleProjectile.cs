using System.Collections.Generic;
using UnityEngine;

public class BibleProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float hitRadius = 0.3f;
    private readonly HashSet<EnemyMovement> checkedEnemies = new();
    private readonly Dictionary<Torch, int> checkedTorches = new();

    internal void MoveAndHit(Vector3 position, KingBibleWeapon weapon)
    {
        checkedEnemies.Clear();
        while (EnemyCrowd.TryGetProjectileHit(transform.position, position, hitRadius,
            out EnemyMovement enemy, checkedEnemies))
        {
            checkedEnemies.Add(enemy);
            weapon.TryHit(enemy);
        }
        checkedTorches.Clear();
        while (Torch.TryGetHit(transform.position, position, hitRadius, out Torch torch, out _, checkedTorches))
        {
            checkedTorches[torch] = torch.ActivationVersion;
            weapon.TryHit(torch);
        }
        transform.SetPositionAndRotation(position, Quaternion.identity);
    }
}
