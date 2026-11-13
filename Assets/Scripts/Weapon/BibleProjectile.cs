using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class BibleProjectile : MonoBehaviour
{
    private static readonly ProfilerMarker hitMarker = new("BibleProjectile.HandleHits");
    [SerializeField, Min(0.01f)] private float hitRadius = 0.3f;
    private readonly HashSet<EnemyMovement> checkedEnemies = new();
    private readonly Dictionary<Torch, int> checkedTorches = new();

    internal void MoveAndHit(Vector3 position, KingBibleWeapon weapon)
    {
        using (hitMarker.Auto())
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
        }
        transform.SetPositionAndRotation(position, Quaternion.identity);
    }
}
