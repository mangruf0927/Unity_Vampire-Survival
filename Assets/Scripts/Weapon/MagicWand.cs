using UnityEngine;

public class MagicWand : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MagicProjectile projectilePrefab;
    [SerializeField, Min(0.01f)] private float cooldown = 1.2f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;

    private float cooldownRemaining;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || projectilePrefab == null) enabled = false;
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining > 0f) return;

        cooldownRemaining = cooldown;
        Fire();
    }

    private void Fire()
    {
        EnemyMovement target = FindNearestVisibleEnemy();
        if (target == null) return;

        Vector3 direction = target.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        MagicProjectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(direction, Vector3.up));
        projectile.Initialize(direction.normalized, projectileSpeed, targetCamera);
    }


    private EnemyMovement FindNearestVisibleEnemy()
    {
        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        EnemyMovement nearest = null;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyMovement enemy = enemies[i];
            if (!enemy.isActiveAndEnabled) continue;
            if (!MagicProjectile.IsInsideView(targetCamera, enemy.transform.position)) continue;

            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearest = enemy;
        }
        return nearest;
    }
}
