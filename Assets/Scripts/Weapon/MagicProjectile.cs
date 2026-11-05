using System.Collections.Generic;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float hitRadius = 0.125f;
    private int damage;
    private int hitsRemaining;
    private float knockback;
    private readonly HashSet<EnemyMovement> hitEnemies = new();

    private Vector3 direction;
    private float speed;
    private Camera targetCamera;

    public void Initialize(Vector3 direction, float speed, Camera targetCamera, int damage, int penetrationCount, float knockback)
    {
        this.direction = direction;
        this.speed = speed;
        this.targetCamera = targetCamera;
        this.damage = damage;
        hitsRemaining = Mathf.Max(1, penetrationCount);
        this.knockback = knockback;
        hitEnemies.Clear();
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

        while (EnemyCrowd.TryGetProjectileHit(start, end, hitRadius, out EnemyMovement hitEnemy, hitEnemies))
        {
            hitEnemies.Add(hitEnemy);
            hitEnemy.ApplyKnockback(direction, knockback);
            if (hitEnemy.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(damage);
            hitsRemaining--;
            if (hitsRemaining <= 0)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position = end;
        if (!IsInsideView(targetCamera, transform.position)) Destroy(gameObject);
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
