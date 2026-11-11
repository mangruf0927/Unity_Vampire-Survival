using System.Collections.Generic;
using UnityEngine;

public class Torch : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.01f)] private float maxHealth = 1f;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.4f;
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
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        broken = false;
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
        SoundManager.Instance?.PlaySFX(SoundManager.TorchBreak);
        ObjectPool.Instance.TryReturnToPool(gameObject);
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
