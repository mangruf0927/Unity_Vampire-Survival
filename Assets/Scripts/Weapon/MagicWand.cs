using UnityEngine;

public class MagicWand : MonoBehaviour, IObserver
{
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private MagicProjectile projectilePrefab;
    [SerializeField, Min(0.01f)] private float cooldown = 1.2f;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;

    [SerializeField, Range(1, 8)] private int level = 1;
    [SerializeField, Min(0)] private int damage = 10;
    [SerializeField, Min(1)] private int projectileCount = 1;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.1f;
    [SerializeField, Min(0f)] private float knockback = 1f;
    [SerializeField, Min(1)] private int penetrationCount = 1;

    public int Level => Mathf.Clamp(level, 1, 8);
    public int Damage => damage + (Level >= 5 ? 10 : 0) + (Level >= 8 ? 10 : 0);
    public int ProjectileCount => projectileCount + (Level >= 2 ? 1 : 0)
        + (Level >= 4 ? 1 : 0) + (Level >= 6 ? 1 : 0);
    public float Cooldown => Mathf.Max(0.01f, cooldown - (Level >= 3 ? 0.2f : 0f));
    public int PenetrationCount => penetrationCount + (Level >= 7 ? 1 : 0);
    public float Knockback => knockback;

    private float cooldownRemaining;
    private float burstRemaining;
    private int shotsRemaining;
    private int lastPlayerLevel = 1;

    private void OnEnable()
    {
        if (playerExperience == null) return;
        playerExperience.AddObserver(this);
        Notify();
    }

    private void OnDisable()
    {
        if (playerExperience != null) playerExperience.RemoveObserver(this);
    }

    public void Notify()
    {
        if (playerExperience == null) return;

        int gainedLevels = playerExperience.Level - lastPlayerLevel;
        lastPlayerLevel = playerExperience.Level;
        for (int i = 0; i < gainedLevels && Level < 8; i++) LevelUp();
    }

    [ContextMenu("Level Up (Play Mode)")]
    public void LevelUp()
    {
        if (!Application.isPlaying || Level >= 8) return;
        level = Level + 1;
    }

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || projectilePrefab == null) enabled = false;
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        // 연사 중에도 쿨타임은 흐르며, 완료 후 추가 쿨타임을 붙이지 않는다.
        cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
        burstRemaining -= Time.deltaTime;
        if (shotsRemaining == 0 && cooldownRemaining <= 0f)
        {
            cooldownRemaining = Cooldown;
            shotsRemaining = ProjectileCount;
            burstRemaining = 0f;
        }

        while (shotsRemaining > 0 && burstRemaining <= 0f)
        {
            Fire();
            shotsRemaining--;
            burstRemaining += Mathf.Max(0.01f, burstInterval);
        }
    }

    private void Fire()
    {
        EnemyMovement target = FindNearestVisibleEnemy();
        if (target == null) return;

        Vector3 direction = target.transform.position - transform.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        MagicProjectile projectile = Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(direction, Vector3.up));
        projectile.Initialize(direction.normalized, projectileSpeed, targetCamera, Damage, PenetrationCount, Knockback);
    }


    private EnemyMovement FindNearestVisibleEnemy()
    {
        EnemyMovement nearest = null;
        float nearestDistance = float.PositiveInfinity;
        Vector3 origin = transform.position;
        for (int i = 0; i < EnemyCrowd.ActiveCount; i++)
        {
            EnemyMovement enemy = EnemyCrowd.GetActiveEnemy(i);
            if (enemy == null || !enemy.isActiveAndEnabled) continue;

            Vector3 position = enemy.transform.position;
            float distance = (position - origin).sqrMagnitude;
            if (distance >= nearestDistance) continue;
            if (!MagicProjectile.IsInsideView(targetCamera, position)) continue;
            nearestDistance = distance;
            nearest = enemy;
        }
        return nearest;
    }
}
