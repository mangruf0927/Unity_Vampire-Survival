using UnityEngine;

public class PickupItem : MonoBehaviour
{
    private enum ItemType { Chicken, Vacuum, Clock }

    [SerializeField] private ItemType itemType;
    [SerializeField, Min(0f)] private float healAmount = 30f;
    [SerializeField, Min(0.01f)] private float attractionSpeed = 10f;
    [SerializeField, Min(0.01f)] private float freezeDuration = 5f;

    private bool collected;

    private void OnEnable() => collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        // 보석 흡인용 자식 센서는 제외하고 플레이어 몸체 접촉만 처리한다.
        if (!other.TryGetComponent(out PlayerExperience player) || !player.isActiveAndEnabled
            || !other.TryGetComponent(out PlayerHealth health) || health.IsDead) return;

        collected = true;
        switch (itemType)
        {
            case ItemType.Chicken:
                health.Heal(healAmount);
                break;
            case ItemType.Vacuum:
                ExperienceGem.AttractAllTo(player, attractionSpeed);
                break;
            case ItemType.Clock:
                EnemyCrowd.FreezeFor(freezeDuration);
                break;
        }
        ObjectPool.Instance.TryReturnToPool(gameObject);
    }
}
