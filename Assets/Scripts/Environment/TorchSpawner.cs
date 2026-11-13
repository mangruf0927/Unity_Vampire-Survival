
using UnityEngine;

public class TorchSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField, Min(0.1f)] private float spawnInterval = 3f;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;
    [SerializeField, Min(0)] private int maximumTorches = 12;
    [SerializeField, Min(0f)] private float spawnMargin = 2f;
    [SerializeField, Min(0f)] private float minimumSpacing = 2f;
    [SerializeField, Min(1)] private int placementAttempts = 8;
    [SerializeField, Min(1f)] private float returnDistance = 70f;
    [SerializeField] private LayerMask groundLayers = 1 << 9;
    [SerializeField] private LayerMask obstacleLayers = 1 << 11;

    private const float CenterHeight = 1f;

    private readonly Vector3[] corners = new Vector3[4];
    private CameraView cameraView;
    private Plane[] cameraPlanes;

    private float timer;
    private float torchRadius;

    private void Start()
    {
        if (gameCamera == null) gameCamera = Camera.main;

        if (player == null || gameCamera == null
            || !gameCamera.TryGetComponent(out cameraView)
            || !ObjectPool.Instance.TryGetPrefab(PoolTypeEnums.Torch, out GameObject prefab)
            || !prefab.TryGetComponent(out Torch torch))
        {
            Debug.LogError("TorchSpawner: Player, Camera의 CameraView와 Torch 풀을 확인해 주세요.", this);
            enabled = false;
            return;
        }

        torchRadius = torch.HitRadius;
    }

    private void LateUpdate()
    {
        if (player == null || Time.timeScale <= 0f) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = spawnInterval;

        cameraPlanes = cameraView.FrustumPlanes;
        ReturnDistantTorches();

        if (CanSpawn()) SpawnTorch();
    }

    private bool CanSpawn()
    {
        return Torch.ActiveCount < maximumTorches && Random.value < spawnChance;
    }

    private void SpawnTorch()
    {
        if (!ReadCameraCorners()) return;

        for (int i = 0; i < placementAttempts; i++)
        {
            if (!TryGetPosition(out Vector3 position)) continue;

            GameObject obj = ObjectPool.Instance.GetFromPool(PoolTypeEnums.Torch, position, Quaternion.identity);
            if (obj == null) return;

            // 불꽃을 포함해 횃불이 화면 밖에 있으면 생성 유지
            if (!obj.GetComponent<Torch>().IsVisible(cameraPlanes)) return;

            ObjectPool.Instance.TryReturnToPool(obj);
        }
    }

    private bool ReadCameraCorners()
    {
        return cameraView.TryGetCorners(CenterHeight, corners, out _);
    }

    private bool TryGetPosition(out Vector3 position)
    {
        int edge = Random.Range(0, 4);
        Vector3 from = corners[edge];
        Vector3 to = corners[(edge + 1) % 4];
        Vector3 point = Vector3.Lerp(from, to, Random.value);
        Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;

        Vector3 outward = new Vector3(-(to - from).z, 0f, (to - from).x).normalized;
        if (Vector3.Dot(outward, point - center) < 0f) outward = -outward;

        position = point + outward * (spawnMargin + torchRadius);

        if (!Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit,
            10f, groundLayers, QueryTriggerInteraction.Ignore)) return false;

        position.y = hit.point.y + CenterHeight;

        if (Physics.CheckCapsule(position + Vector3.down * 0.5f, position + Vector3.up * 0.5f,
            torchRadius, obstacleLayers, QueryTriggerInteraction.Ignore)) return false;

        return Torch.IsSpaceAvailable(position, torchRadius, minimumSpacing);
    }

    private void ReturnDistantTorches()
    {
        float distanceSquared = returnDistance * returnDistance;

        for (int i = Torch.ActiveCount - 1; i >= 0; i--)
        {
            Torch torch = Torch.GetActiveTorch(i);
            Vector3 offset = torch.transform.position - player.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > distanceSquared && !torch.IsVisible(cameraPlanes))
                ObjectPool.Instance.TryReturnToPool(torch.gameObject);
        }
    }
}
