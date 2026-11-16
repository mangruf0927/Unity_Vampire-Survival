
using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float hitRadius = 0.4f;

    [Header("Drop probabilities (%) - automatically normalized to 100")]
    [SerializeField, Range(0f, 100f)] private float noDropPercent = 50f;
    [SerializeField, Range(0f, 100f)] private float chickenDropPercent = 30f;
    [SerializeField, Range(0f, 100f)] private float vacuumDropPercent = 12.5f;
    [SerializeField, Range(0f, 100f)] private float clockDropPercent = 7.5f;
    [SerializeField] private LayerMask groundLayers = 1 << 9;

    private static readonly List<Torch> activeTorches = new();

    private readonly List<MeshFilter> dropMeshes = new();
    private Renderer[] renderers;
    private MeshRenderer[] bodyRenderers;
    private float groundHeight;
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
        NormalizeDropRates();
        renderers = GetComponentsInChildren<Renderer>();
        bodyRenderers = GetComponentsInChildren<MeshRenderer>();
    }

    private void OnEnable()
    {
        broken = false;
        groundHeight = renderers.Length > 0 ? renderers[0].bounds.min.y : transform.position.y;
        for (int i = 1; i < renderers.Length; i++) groundHeight = Mathf.Min(groundHeight, renderers[i].bounds.min.y);

        ActivationVersion++;
        activeTorches.Add(this);
    }

    private void OnDisable()
    {
        activeTorches.Remove(this);
    }

    internal void PlaceOnGround(float groundY)
    {
        float lowest = float.PositiveInfinity;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            MeshRenderer renderer = bodyRenderers[i];
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        }
        if (lowest == float.PositiveInfinity) return;

        // 명중 판정에 사용하는 루트는 유지하고, 모델과 불꽃만 함께 지면에 맞춘다.
        Vector3 offset = Vector3.up * (groundY - lowest);
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).position += offset;
        groundHeight = groundY;
    }

    public void TakeDamage(float damage)
    {
        if (broken || !isActiveAndEnabled || damage <= 0f || Time.timeScale <= 0f) return;

        broken = true;
        DropItem();
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.TorchBreak);
        ObjectPool.Instance.TryReturnToPool(gameObject);
    }

    private void OnValidate()
    {
        NormalizeDropRates();
    }

    private void NormalizeDropRates()
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
        if (!TrySelectDrop(out PoolTypeEnums type)) return;
        SpawnDrop(type);
    }

    private bool TrySelectDrop(out PoolTypeEnums type)
    {
        type = default;

        float total = noDropPercent + chickenDropPercent + vacuumDropPercent + clockDropPercent;
        if (total <= 0f) return false;

        float roll = Random.value * total;
        if (roll < noDropPercent) return false;
        roll -= noDropPercent;

        if (chickenDropPercent > 0f && roll < chickenDropPercent) type = PoolTypeEnums.Chicken;
        else if (vacuumDropPercent > 0f && roll < chickenDropPercent + vacuumDropPercent) type = PoolTypeEnums.Vacuum;
        else if (clockDropPercent > 0f) type = PoolTypeEnums.Clock;
        else if (vacuumDropPercent > 0f) type = PoolTypeEnums.Vacuum;
        else if (chickenDropPercent > 0f) type = PoolTypeEnums.Chicken;
        else return false;

        return true;
    }

    private void SpawnDrop(PoolTypeEnums type)
    {
        Vector3 position = transform.position;
        float height = groundHeight;

        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit,
            Mathf.Infinity, groundLayers, QueryTriggerInteraction.Ignore)) height = hit.point.y;

        position.y = height;
        GameObject item = ObjectPool.Instance.GetFromPool(type, position, Quaternion.identity);
        if (item != null) PlaceDropOnGround(item, height);
    }

    private void PlaceDropOnGround(GameObject item, float groundY)
    {
        dropMeshes.Clear();
        item.GetComponentsInChildren<MeshFilter>(true, dropMeshes);
        float lowest = float.PositiveInfinity;

        for (int i = 0; i < dropMeshes.Count; i++)
        {
            MeshFilter mesh = dropMeshes[i];
            if (mesh.sharedMesh == null) continue;
            Bounds bounds = mesh.sharedMesh.bounds;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new((corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                lowest = Mathf.Min(lowest, mesh.transform.TransformPoint(point).y);
            }
        }
        if (lowest < float.PositiveInfinity)
            item.transform.position += Vector3.up * (groundY - lowest);
        dropMeshes.Clear();
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

    internal static bool TryGetHit(Vector3 start, Vector3 end, float attackRadius, out Torch hitTorch, out float hitTime, Dictionary<Torch, int> ignored)
    {
        hitTorch = null;
        hitTime = float.PositiveInfinity;

        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;

        for (int i = 0; i < activeTorches.Count; i++)
        {
            Torch torch = activeTorches[i];
            if (!IsValidTarget(torch, ignored)) continue;
            if (!TryCalculateHitTime(torch, start, segment, lengthSquared, attackRadius, out float first)) continue;
            if (first >= hitTime) continue;

            hitTime = first;
            hitTorch = torch;
        }

        return hitTorch != null;
    }

    private static bool IsValidTarget(Torch torch, Dictionary<Torch, int> ignored)
    {
        if (!torch.isActiveAndEnabled || torch.broken) return false;
        if (ignored.TryGetValue(torch, out int version) && version == torch.ActivationVersion) return false;
        return true;
    }

    private static bool TryCalculateHitTime(Torch torch, Vector3 start, Vector3 segment, float lengthSquared, float attackRadius, out float hitTime)
    {
        hitTime = 0f;

        Vector3 offset = start - torch.transform.position;
        float radius = attackRadius + torch.hitRadius;
        float t = lengthSquared > 0f ? Mathf.Clamp01(-Vector3.Dot(offset, segment) / lengthSquared) : 0f;

        if ((offset + segment * t).sqrMagnitude > radius * radius) return false;
        if (lengthSquared <= 0f) return true;

        float b = Vector3.Dot(offset, segment);
        float c = offset.sqrMagnitude - radius * radius;
        hitTime = Mathf.Max(0f, (-b - Mathf.Sqrt(Mathf.Max(0f, b * b - lengthSquared * c))) / lengthSquared);

        return true;
    }
}
