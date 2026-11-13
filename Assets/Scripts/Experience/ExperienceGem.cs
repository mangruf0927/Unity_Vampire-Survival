using System.Collections.Generic;
using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    private static readonly List<ExperienceGem> activeGems = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGems() => activeGems.Clear();

    public static void AttractAllTo(PlayerExperience player, float speed)
    {
        for (int i = 0; i < activeGems.Count; i++) activeGems[i].AttractTo(player, speed);
    }

    [SerializeField, Min(1)] private int dataId = 1;
    private int experienceValue;
    public float GroundOffset { get; private set; }

    private bool collected;
    private PlayerExperience attractionTarget;
    private float attractionSpeed;

    private void Awake()
    {
        experienceValue = DataManager.Instance.Gems.Get(dataId).experienceValue;
        CalculateGroundOffset();
    }

    private void CalculateGroundOffset()
    {
        // 모델이 자식에 있어도 크기·회전·피벗을 포함한 최하단 높이를 사용한다.
        // 풀 생성 시 한 번 계산하여 사망할 때마다 배열을 만들지 않는다.
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>(true);
        float lowest = float.PositiveInfinity;
        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i].sharedMesh == null) continue;
            Bounds bounds = meshes[i].sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                lowest = Mathf.Min(lowest, meshes[i].transform.TransformPoint(point).y);
            }
        }
        GroundOffset = lowest < float.PositiveInfinity ? transform.position.y - lowest : 0f;
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

    public void AttractTo(PlayerExperience player, float speed)
    {
        if (collected || !isActiveAndEnabled || player == null || !player.isActiveAndEnabled) return;
        // 일반 획득 센서에 닿아도 이미 흡입 중인 속도를 낮추지 않는다.
        attractionSpeed = attractionTarget == player ? Mathf.Max(attractionSpeed, speed) : Mathf.Max(0f, speed);
        attractionTarget = player;
    }

    private void Update()
    {
        if (collected || attractionTarget == null || !attractionTarget.isActiveAndEnabled
            || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
        Vector3 targetPosition = attractionTarget.transform.position;
        targetPosition.y = transform.position.y;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, attractionSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        if (!other.TryGetComponent(out PlayerExperience player))
        {
            // 자식 센서 접촉은 끌어당김만 시작하고, 몸체 접촉 시에만 획득한다.
            PlayerExperience owner = other.GetComponentInParent<PlayerExperience>();
            if (owner != null) owner.TryAttractGem(other, this);
            return;
        }
        if (!player.isActiveAndEnabled) return;

        collected = true;
        SoundManager.Instance?.PlaySFX(SoundManager.ExperiencePickup);
        player.AddExperience(Mathf.Max(1, experienceValue));
        ObjectPool.Instance.TryReturnToPool(gameObject);
    }
}
