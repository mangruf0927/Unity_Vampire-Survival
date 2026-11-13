using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class Projectile : MonoBehaviour
{
    private static readonly ProfilerMarker hitMarker = new("Projectile.HandleHits");
    [SerializeField, Min(0f)] private float hitRadius = 0.125f;
    private float damage;
    private int hitsRemaining;
    private float knockback;
    private readonly HashSet<EnemyMovement> hitEnemies = new();
    private readonly Dictionary<Torch, int> hitTorches = new();

    private Vector3 direction;
    private float speed;
    private float hitRadiusMultiplier = 1f;
    private Camera targetCamera;

    public void Initialize(Vector3 direction, float speed, Camera targetCamera, float damage, int penetrationCount, float knockback, float areaMultiplier = 1f)
    {
        this.direction = direction;
        this.speed = speed;
        this.targetCamera = targetCamera;
        this.damage = damage;
        hitsRemaining = Mathf.Max(1, penetrationCount);
        this.knockback = knockback;
        hitRadiusMultiplier = Mathf.Max(0f, areaMultiplier);
        hitEnemies.Clear();
        hitTorches.Clear();
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        if (targetCamera == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 start = transform.position;
        Vector3 end = start + direction * speed * Time.deltaTime;

        if (HandleProjectileHits(start, end)) return;

        transform.position = end;
        if (!IsInsideView(targetCamera, end)) Destroy(gameObject);
    }

    private bool HandleProjectileHits(Vector3 start, Vector3 end)
    {
        using var scope = hitMarker.Auto();
        while (true)
        {
            float radius = hitRadius * hitRadiusMultiplier;
            bool hasEnemy = EnemyCrowd.TryGetProjectileHit(start, end, radius,
                out EnemyMovement enemy, out float enemyTime, hitEnemies);
            bool hasTorch = Torch.TryGetHit(start, end, radius, out Torch torch, out float torchTime, hitTorches);
            if (!hasEnemy && !hasTorch) break;
            if (hasEnemy && (!hasTorch || enemyTime <= torchTime))
            {
                hitEnemies.Add(enemy);
                enemy.ApplyKnockback(direction, knockback);
                if (enemy.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(damage);
            }
            else
            {
                hitTorches[torch] = torch.ActivationVersion;
                torch.TakeDamage(damage);
            }

            if (--hitsRemaining <= 0)
            {
                Destroy(gameObject);
                return true;
            }
        }

        return false;
    }

    internal static bool IsInsideView(Camera camera, Vector3 position)
    {
        if (camera == null) return false;
        Vector3 viewport = camera.WorldToViewportPoint(position);
        return viewport.z >= camera.nearClipPlane && viewport.z <= camera.farClipPlane
            && viewport.x >= 0f && viewport.x <= 1f
            && viewport.y >= 0f && viewport.y <= 1f;
    }
}
