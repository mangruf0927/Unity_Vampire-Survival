using System.Collections.Generic;
using UnityEngine;

public class BibleProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float hitRadius = 0.3f;
    private readonly HashSet<EnemyMovement> checkedEnemies = new();

    internal void MoveAndHit(Vector3 position, KingBible weapon)
    {
        checkedEnemies.Clear();
        while (EnemyCrowd.TryGetProjectileHit(transform.position, position, hitRadius,
            out EnemyMovement enemy, checkedEnemies))
        {
            checkedEnemies.Add(enemy);
            weapon.TryHit(enemy);
        }
        transform.position = position;
        transform.rotation = Quaternion.identity;
    }
}
