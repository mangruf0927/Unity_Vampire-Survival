
using System.Collections.Generic;
using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField, Min(1)] private int dataId = 1;

    private static readonly List<ExperienceGem> activeGems = new();
    private const float VacuumAttractionDuration = 1f;

    private PlayerExperience attractionTarget;
    private int experienceValue;
    private bool collected;
    private float attractionSpeed;
    private float vacuumTimeRemaining;

    public float GroundOffset { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGems() => activeGems.Clear();

    public static void AttractAllTo(PlayerExperience player, float speed)
    {
        if (player == null || !player.isActiveAndEnabled) return;

        for (int i = 0; i < activeGems.Count; i++)
        {
            ExperienceGem gem = activeGems[i];
            gem.AttractTo(player, speed);
            if (gem.collected) continue;

            gem.vacuumTimeRemaining = gem.vacuumTimeRemaining > 0f
                ? Mathf.Min(gem.vacuumTimeRemaining, VacuumAttractionDuration)
                : VacuumAttractionDuration;
        }
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
        vacuumTimeRemaining = 0f;
    }

    private void OnDisable()
    {
        activeGems.Remove(this);
        attractionTarget = null;
        vacuumTimeRemaining = 0f;
    }

    private void Update()
    {
        if (collected || attractionTarget == null || !attractionTarget.isActiveAndEnabled || Time.timeScale <= 0f || Time.deltaTime <= 0f)
            return;

        Vector3 targetPosition = attractionTarget.transform.position;
        targetPosition.y = transform.position.y;
        float speed = attractionSpeed;
        bool isVacuumAttraction = vacuumTimeRemaining > 0f;
        if (isVacuumAttraction)
        {
            float distance = Vector3.Distance(transform.position, targetPosition);
            speed = Mathf.Max(speed, distance / vacuumTimeRemaining);
            vacuumTimeRemaining = Mathf.Max(0f, vacuumTimeRemaining - Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // 빠른 이동으로 물리 접촉을 놓쳐도 도착한 보석은 기존 획득 경로로 처리한다.
        if (isVacuumAttraction && (vacuumTimeRemaining <= 0f || transform.position == targetPosition))
        {
            transform.position = targetPosition;
            Collect(attractionTarget);
        }
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
