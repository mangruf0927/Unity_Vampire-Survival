using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float maxHealth = 1f;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.4f;
    [Header("Drop probabilities (%) - automatically normalized to 100")]
    [SerializeField, Range(0f, 100f)] private float noDropPercent = 50f;
    [SerializeField, Range(0f, 100f)] private float chickenDropPercent = 30f;
    [SerializeField, Range(0f, 100f)] private float vacuumDropPercent = 12.5f;
    [SerializeField, Range(0f, 100f)] private float clockDropPercent = 7.5f;
    [SerializeField] private LayerMask groundLayers = 1 << 9;
    private float groundHeight;
    private static readonly List<Torch> activeTorches = new();
    private Renderer[] renderers;
    private float currentHealth;
    private bool broken;
    internal int ActivationVersion { get; private set; }
    internal float HitRadius => hitRadius;
    internal static int ActiveCount => activeTorches.Count;
    internal static Torch GetActiveTorch(int index) => activeTorches[index];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTorches()
    {
        activeTorches.Clear();
    }

    private void Awake()
    {
        NormalizeDropProbabilities();
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        broken = false;
        // 바닥 청크가 회수되어도 마지막 배치 높이를 드롭에 사용할 수 있다.
        groundHeight = renderers.Length > 0 ? renderers[0].bounds.min.y : transform.position.y;
        for (int i = 1; i < renderers.Length; i++) groundHeight = Mathf.Min(groundHeight, renderers[i].bounds.min.y);
        ActivationVersion++;
        activeTorches.Add(this);
    }

    private void OnDisable()
    {
        activeTorches.Remove(this);
    }

    public void TakeDamage(float damage)
    {
        if (broken || !isActiveAndEnabled || damage <= 0f || Time.timeScale <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (currentHealth > 0f) return;
        broken = true;
        DropItem();
        SoundManager.Instance?.PlaySFX(SoundManager.TorchBreak);
        ObjectPool.Instance.TryReturnToPool(gameObject);
    }

    private void OnValidate()
    {
        NormalizeDropProbabilities();
    }

    private void NormalizeDropProbabilities()
    {
        noDropPercent = Mathf.Clamp(noDropPercent, 0f, 100f);
        chickenDropPercent = Mathf.Clamp(chickenDropPercent, 0f, 100f);
        vacuumDropPercent = Mathf.Clamp(vacuumDropPercent, 0f, 100f);
        clockDropPercent = Mathf.Clamp(clockDropPercent, 0f, 100f);
        float total = noDropPercent + chickenDropPercent + vacuumDropPercent + clockDropPercent;
        if (total <= 0f)
        {
            noDropPercent = 100f;
            return;
        }
        float scale = 100f / total;
        noDropPercent *= scale;
        chickenDropPercent *= scale;
        vacuumDropPercent *= scale;
        clockDropPercent *= scale;
    }

    private void DropItem()
    {
        float total = noDropPercent + chickenDropPercent + vacuumDropPercent + clockDropPercent;
        float roll = Random.value * total;
        if (roll < noDropPercent || total <= 0f) return;
        roll -= noDropPercent;

        PoolTypeEnums type;
        if (chickenDropPercent > 0f && roll < chickenDropPercent) type = PoolTypeEnums.Chicken;
        else if (vacuumDropPercent > 0f && roll < chickenDropPercent + vacuumDropPercent) type = PoolTypeEnums.Vacuum;
        else if (clockDropPercent > 0f) type = PoolTypeEnums.Clock;
        // Random.value가 1인 경우에도 확률 0인 종류를 선택하지 않는다.
        else if (vacuumDropPercent > 0f) type = PoolTypeEnums.Vacuum;
        else if (chickenDropPercent > 0f) type = PoolTypeEnums.Chicken;
        else return;

        if (!ObjectPool.Instance.TryGetPrefab(type, out GameObject prefab)
            || !prefab.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null) return;
        Vector3 position = transform.position;
        float height = groundHeight;
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit,
            Mathf.Infinity, groundLayers, QueryTriggerInteraction.Ignore)) height = hit.point.y;
        float halfHeight = meshFilter.sharedMesh.bounds.extents.y * Mathf.Abs(prefab.transform.localScale.y);
        position.y = height + halfHeight;
        ObjectPool.Instance.GetFromPool(type, position, Quaternion.identity);
    }

    internal bool IsVisible(Plane[] planes)
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i].enabled && GeometryUtility.TestPlanesAABB(planes, renderers[i].bounds)) return true;
        return false;
    }

    internal static bool IsSpaceAvailable(Vector3 position, float radius, float spacing)
    {
        for (int i = 0; i < activeTorches.Count; i++)
        {
            Torch other = activeTorches[i];
            Vector3 offset = position - other.transform.position;
            offset.y = 0f;
            float minimum = radius + other.hitRadius + spacing;
            if (offset.sqrMagnitude < minimum * minimum) return false;
        }
        return true;
    }

    internal static bool TryGetHit(Vector3 start, Vector3 end, float attackRadius,
        out Torch hitTorch, out float hitTime, Dictionary<Torch, int> ignored)
    {
        hitTorch = null;
        hitTime = float.PositiveInfinity;
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        for (int i = 0; i < activeTorches.Count; i++)
        {
            Torch torch = activeTorches[i];
            if (!torch.isActiveAndEnabled || torch.broken) continue;
            if (ignored.TryGetValue(torch, out int version) && version == torch.ActivationVersion) continue;
            Vector3 offset = start - torch.transform.position;
            float radius = attackRadius + torch.hitRadius;
            float t = lengthSquared > 0f ? Mathf.Clamp01(-Vector3.Dot(offset, segment) / lengthSquared) : 0f;
            if ((offset + segment * t).sqrMagnitude > radius * radius) continue;
            float first = 0f;
            if (lengthSquared > 0f)
            {
                float b = Vector3.Dot(offset, segment);
                float c = offset.sqrMagnitude - radius * radius;
                first = Mathf.Max(0f, (-b - Mathf.Sqrt(Mathf.Max(0f, b * b - lengthSquared * c))) / lengthSquared);
            }
            if (first >= hitTime) continue;
            hitTime = first;
            hitTorch = torch;
        }
        return hitTorch != null;
    }
}
