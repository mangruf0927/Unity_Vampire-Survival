using UnityEngine;

public class MapAccessoryPickup : MonoBehaviour
{
    [SerializeField] private AccessoryManager player;
    [SerializeField] private Accessory accessoryPrefab;
    [SerializeField] private Vector2 startOffset;
    [SerializeField] private float groundHeight;
    private bool collected;

    private void Start()
    {
        if (player == null || accessoryPrefab == null)
        {
            Debug.LogError("MapAccessoryPickup: Player와 Accessory Prefab을 연결해 주세요.", this);
            gameObject.SetActive(false);
            return;
        }

        Vector3 origin = player.transform.position;
        transform.position = new Vector3(origin.x + startOffset.x, groundHeight, origin.z + startOffset.y);
        // 먼 위치의 바닥 청크가 아직 없어도 정해진 지면 높이에 놓는다.
        if (TryGetComponent(out Renderer display))
            transform.position += Vector3.up * (groundHeight - display.bounds.min.y);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        // 보석을 끌어당기는 자식 센서에는 AccessoryManager가 없으므로 획득하지 않는다.
        if (!other.TryGetComponent(out AccessoryManager owner) || owner != player
            || !owner.isActiveAndEnabled || !other.TryGetComponent(out PlayerHealth health) || health.IsDead) return;

        collected = true;
        if (!owner.AcquireFromMap(accessoryPrefab))
        {
            collected = false;
            return;
        }
        // 청크와 독립된 일회성 배치이므로 재생성하거나 풀로 반환하지 않는다.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
