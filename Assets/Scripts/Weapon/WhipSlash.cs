using System.Collections.Generic;
using UnityEngine;

public class WhipSlash : MonoBehaviour
{

    private readonly HashSet<EnemyMovement> hitEnemies = new();
    private readonly Dictionary<Torch, int> hitTorches = new();
    private Transform origin;
    private Vector3 direction;
    private float damage;
    private float knockback;
    private float length;
    private float width;
    private float remainingDuration;

    public void Initialize(Transform origin, Vector3 direction, float damage, float knockback,
        float length, float width, float duration)
    {
        this.origin = origin;
        this.direction = direction;
        this.damage = damage;
        this.knockback = knockback;
        this.length = Mathf.Max(length, width);
        this.width = width;
        remainingDuration = duration;
        hitEnemies.Clear();
        hitTorches.Clear();
        UpdateSlash();
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;
        if (origin == null)
        {
            Destroy(gameObject);
            return;
        }
        remainingDuration -= Time.deltaTime;
        if (remainingDuration <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        UpdateSlash();
    }

    private void UpdateSlash()
    {
        transform.SetPositionAndRotation(origin.position + direction * (length * 0.5f), Quaternion.LookRotation(direction, Vector3.up));
        transform.localScale = new Vector3(width, 0.12f, length);

        float radius = width * 0.5f;
        Vector3 start = origin.position + direction * radius;
        Vector3 end = origin.position + direction * (length - radius);
        while (EnemyCrowd.TryGetProjectileHit(start, end, radius, out EnemyMovement enemy, hitEnemies))
        {
            hitEnemies.Add(enemy);
            enemy.ApplyKnockback(direction, knockback);
            if (enemy.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(damage);
        }
        while (Torch.TryGetHit(start, end, radius, out Torch torch, out _, hitTorches))
        {
            hitTorches[torch] = torch.ActivationVersion;
            torch.TakeDamage(damage);
        }
    }
}
