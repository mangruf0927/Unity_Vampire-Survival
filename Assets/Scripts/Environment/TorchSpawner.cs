
using UnityEngine;

public class TorchSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField, Min(1)] private int dataId = 1;
    [SerializeField] private LayerMask groundLayers = 1 << 9;
    [SerializeField] private LayerMask obstacleLayers = 1 << 11;

    private const float CenterHeight = 1f;

    private readonly Vector3[] corners = new Vector3[4];
    private CameraView cameraView;
    private TorchSpawnData data;
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

        data = DataManager.Instance.TorchSpawns.Get(dataId);
        torchRadius = torch.HitRadius;
    }

    private void LateUpdate()
    {
        if (player == null || Time.timeScale <= 0f) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = data.spawnInterval;

        Plane[] planes = cameraView.FrustumPlanes;
        ReturnDistantTorches(planes);

        if (CanSpawn()) SpawnTorch(planes);
    }

    private bool CanSpawn()
    {
        return Torch.ActiveCount < data.maximumTorches && Random.value < data.spawnChance;
    }

    private void SpawnTorch(Plane[] planes)
    {
        if (!ReadCameraCorners()) return;

        for (int i = 0; i < data.placementAttempts; i++)
        {
            if (!TryGetPosition(out Vector3 position)) continue;

            GameObject obj = ObjectPool.Instance.GetFromPool(PoolTypeEnums.Torch, position, Quaternion.identity);
            if (obj == null) return;

            Torch torch = obj.GetComponent<Torch>();
            torch.PlaceOnGround(position.y - CenterHeight);
            if (!torch.IsVisible(planes)) return;

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

        position = point + outward * (data.spawnMargin + torchRadius);

        if (!Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, groundLayers, QueryTriggerInteraction.Ignore))
            return false;

        position.y = hit.point.y + CenterHeight;

        if (Physics.CheckCapsule(position + Vector3.down * 0.5f, position + Vector3.up * 0.5f, torchRadius, obstacleLayers, QueryTriggerInteraction.Ignore))
            return false;

        return Torch.IsSpaceAvailable(position, torchRadius, data.minimumSpacing);
    }

    private void ReturnDistantTorches(Plane[] planes)
    {
        float distanceSquared = data.returnDistance * data.returnDistance;

        for (int i = Torch.ActiveCount - 1; i >= 0; i--)
        {
            Torch torch = Torch.GetActiveTorch(i);
            Vector3 offset = torch.transform.position - player.position;
            offset.y = 0f;

            if (offset.sqrMagnitude > distanceSquared && !torch.IsVisible(planes))
                ObjectPool.Instance.TryReturnToPool(torch.gameObject);
        }
    }
}
