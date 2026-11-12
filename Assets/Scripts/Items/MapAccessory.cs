using UnityEngine;

public class MapAccessory : MonoBehaviour
{
    [SerializeField] private Accessory accessoryPrefab;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        if (!other.TryGetComponent(out AccessoryManager owner) || !owner.isActiveAndEnabled) return;
        if (!other.TryGetComponent(out PlayerHealth health) || health.IsDead) return;

        collected = true;
        if (!owner.AcquireOrUpgrade(accessoryPrefab, true))
        {
            collected = false;
            return;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
