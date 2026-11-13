using UnityEngine;

public class MapItem : MonoBehaviour
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

        if (!other.TryGetComponent(out PlayerExperience player) || !player.isActiveAndEnabled
            || !other.TryGetComponent(out PlayerHealth health) || health.IsDead) return;

        collected = true;

        if (itemType == ItemType.Chicken) health.Heal(healAmount);
        else if (itemType == ItemType.Vacuum) ExperienceGem.AttractAllTo(player, attractionSpeed);
        else if (itemType == ItemType.Clock) EnemyCrowd.FreezeFor(freezeDuration);

        ObjectPool.Instance.TryReturnToPool(gameObject);
    }
}
