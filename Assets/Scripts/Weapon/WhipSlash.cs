using System.Collections.Generic;
using UnityEngine;

public class WhipSlash : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Mesh visualMesh;

    // 현재 Slash VFX의 기본 size 0.1 × 성장 곡선의 최댓값 1.
    private const float VisualMeshScale = 0.1f;
    private readonly Dictionary<EnemyMovement, int> hitEnemies = new();
    private readonly Dictionary<Torch, int> hitTorches = new();
    private readonly Dictionary<EnemyMovement, int> checkedEnemies = new();
    private readonly Dictionary<Torch, int> checkedTorches = new();
    private Transform origin;
    private Vector3 direction;
    private float damage;
    private float knockback;
    private float length;
    private float width;
    private float remainingDuration;

    public void Initialize(Transform origin, Vector3 direction, float damage, float knockback, float length, float width, float duration)
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

        Bounds hitBounds = new Bounds(Vector3.zero, new Vector3(width, width, length));
        Quaternion inverseRotation = Quaternion.Inverse(transform.rotation);
        if (visualRoot != null && visualMesh != null)
        {
            Bounds meshBounds = visualMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3((corner & 1) == 0 ? meshBounds.min.x : meshBounds.max.x,
                                           (corner & 2) == 0 ? meshBounds.min.y : meshBounds.max.y,
                                           (corner & 4) == 0 ? meshBounds.min.z : meshBounds.max.z);
                Vector3 worldPoint = visualRoot.TransformPoint(point * VisualMeshScale);
                hitBounds.Encapsulate(inverseRotation * (worldPoint - transform.position));
            }
        }

        // 격자로 후보를 찾은 뒤 이펙트 방향의 박스와 대상 반경으로 최종 판정한다.
        Vector3 center = transform.position + transform.rotation * hitBounds.center;
        float radius = hitBounds.extents.magnitude;
        checkedEnemies.Clear();
        checkedTorches.Clear();
        while (EnemyCrowd.TryGetProjectileHit(center, center, radius, out EnemyMovement enemy, ignoredEnemyVersions: checkedEnemies))
        {
            checkedEnemies[enemy] = enemy.ActivationVersion;
            if (hitEnemies.TryGetValue(enemy, out int version) && version == enemy.ActivationVersion) continue;
            Vector3 localPosition = inverseRotation * (enemy.transform.position - transform.position);
            if (hitBounds.SqrDistance(localPosition) > enemy.BodyRadius * enemy.BodyRadius) continue;

            hitEnemies[enemy] = enemy.ActivationVersion;
            enemy.ApplyKnockback(direction, knockback);
            if (enemy.TryGetComponent(out IDamageable damageable)) damageable.TakeDamage(damage);
        }
        while (Torch.TryGetHit(center, center, radius, out Torch torch, out _, checkedTorches))
        {
            checkedTorches[torch] = torch.ActivationVersion;
            if (hitTorches.TryGetValue(torch, out int version) && version == torch.ActivationVersion) continue;
            Vector3 localPosition = inverseRotation * (torch.transform.position - transform.position);
            if (hitBounds.SqrDistance(localPosition) > torch.HitRadius * torch.HitRadius) continue;

            hitTorches[torch] = torch.ActivationVersion;
            torch.TakeDamage(damage);
        }
    }
}
