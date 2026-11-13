
using System.Collections.Generic;
using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField, Min(1)] private int dataId = 1;

    private static readonly List<ExperienceGem> activeGems = new();

    private PlayerExperience attractionTarget;
    private int experienceValue;
    private bool collected;
    private float attractionSpeed;

    public float GroundOffset { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGems() => activeGems.Clear();

    public static void AttractAllTo(PlayerExperience player, float speed)
    {
        for (int i = 0; i < activeGems.Count; i++) activeGems[i].AttractTo(player, speed);
    }

    private void Awake()
    {
        experienceValue = DataManager.Instance.Gems.Get(dataId).experienceValue;
        CalculateGroundOffset();
    }

    private void OnEnable()
    {
        activeGems.Add(this);
        collected = false;
        attractionTarget = null;
        attractionSpeed = 0f;
    }

    private void OnDisable()
    {
        activeGems.Remove(this);
        attractionTarget = null;
    }

    private void Update()
    {
        if (collected || attractionTarget == null || !attractionTarget.isActiveAndEnabled || Time.timeScale <= 0f || Time.deltaTime <= 0f)
            return;

        Vector3 targetPosition = attractionTarget.transform.position;
        targetPosition.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, attractionSpeed * Time.deltaTime);
    }

    public void AttractTo(PlayerExperience player, float speed)
    {
        if (collected || !isActiveAndEnabled || player == null || !player.isActiveAndEnabled) return;

        attractionSpeed = attractionTarget == player ? Mathf.Max(attractionSpeed, speed) : Mathf.Max(0f, speed);
        attractionTarget = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !isActiveAndEnabled || Time.timeScale <= 0f) return;

        if (other.TryGetComponent(out PlayerExperience player))
        {
            if (player.isActiveAndEnabled) Collect(player);
            return;
        }

        PlayerExperience owner = other.GetComponentInParent<PlayerExperience>();
        if (owner != null) owner.TryAttractGem(other, this);
    }

    private void Collect(PlayerExperience player)
    {
        collected = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.ExperiencePickup);
        player.AddExperience(Mathf.Max(1, experienceValue));
        ObjectPool.Instance.TryReturnToPool(gameObject);
    }

    private void CalculateGroundOffset()
    {
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>(true);
        float lowest = float.PositiveInfinity;

        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i].sharedMesh == null) continue;

            Bounds bounds = meshes[i].sharedMesh.bounds;

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new((corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);

                lowest = Mathf.Min(lowest, meshes[i].transform.TransformPoint(point).y);
            }
        }
        GroundOffset = lowest < float.PositiveInfinity ? transform.position.y - lowest : 0f;
    }
}
