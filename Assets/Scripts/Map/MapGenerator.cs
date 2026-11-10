using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Vector2 chunkSize = new(32f, 32f);
    [SerializeField, Min(1)] private int keepRadius = 1;
    [SerializeField, Min(0f)] private float viewPadding = 8f;

    private readonly Dictionary<Vector2Int, GameObject> activeChunks = new();
    private readonly List<Vector2Int> returnCoordinates = new();
    private Vector3 origin;
    private Vector2Int previousCenter;
    private int previousRadius = -1;

    private void Start()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (player == null || gameCamera == null || chunkSize.x <= 0f || chunkSize.y <= 0f
            || !ObjectPool.Instance.TryGetPrefab(PoolTypeEnums.ForestChunk, out _))
        {
            Debug.LogError("MapGenerator: Player, Camera, Chunk Size와 ForestChunk 풀을 확인해 주세요.", this);
            enabled = false;
            return;
        }
        origin = transform.position;
        RefreshChunks();
    }

    private void LateUpdate()
    {
        if (player == null || Time.timeScale <= 0f) return;
        RefreshChunks();
    }

    private void RefreshChunks()
    {
        Vector2Int center = new(Mathf.FloorToInt((player.position.x - origin.x) / chunkSize.x), Mathf.FloorToInt((player.position.z - origin.z) / chunkSize.y));
        int radius = GetRequiredRadius();
        if (center == previousCenter && radius == previousRadius) return;

        returnCoordinates.Clear();
        foreach (KeyValuePair<Vector2Int, GameObject> pair in activeChunks)
        {
            if (Mathf.Abs(pair.Key.x - center.x) > radius || Mathf.Abs(pair.Key.y - center.y) > radius)
                returnCoordinates.Add(pair.Key);
        }
        // 먼저 반환해 경계 이동 때 새 청크를 만들지 않고 재사용한다.
        for (int i = 0; i < returnCoordinates.Count; i++)
        {
            Vector2Int coordinate = returnCoordinates[i];
            ObjectPool.Instance.TryReturnToPool(activeChunks[coordinate]);
            activeChunks.Remove(coordinate);
        }
        for (int z = center.y - radius; z <= center.y + radius; z++)
        {
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                Vector2Int coordinate = new(x, z);
                if (activeChunks.ContainsKey(coordinate)) continue;
                Vector3 position = origin + new Vector3(x * chunkSize.x, 0f, z * chunkSize.y);
                GameObject chunk = ObjectPool.Instance.GetFromPool(PoolTypeEnums.ForestChunk, position, Quaternion.identity);
                if (chunk == null) return;
                activeChunks.Add(coordinate, chunk);
            }
        }
        previousCenter = center;
        previousRadius = radius;
        // 배치 직후의 바닥 Collider를 보석 생성 Raycast에도 반영한다.
        Physics.SyncTransforms();
    }

    private int GetRequiredRadius()
    {
        int radius = Mathf.Max(1, keepRadius);
        Plane ground = new(Vector3.up, origin);
        for (int i = 0; i < 4; i++)
        {
            Ray ray = gameCamera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0f));
            if (!ground.Raycast(ray, out float distance)) continue;
            Vector3 point = ray.GetPoint(distance);
            // 플레이어가 청크 가장자리에 있어도 화면을 덮도록 여유 반경을 계산한다.
            float x = (Mathf.Abs(point.x - player.position.x) + viewPadding) / chunkSize.x;
            float z = (Mathf.Abs(point.z - player.position.z) + viewPadding) / chunkSize.y;
            radius = Mathf.Max(radius, Mathf.CeilToInt(Mathf.Max(x, z)));
        }
        return radius;
    }

    private void OnDestroy()
    {
        foreach (GameObject chunk in activeChunks.Values)
            if (chunk != null) ObjectPool.Instance.TryReturnToPool(chunk);
        activeChunks.Clear();
    }
}
