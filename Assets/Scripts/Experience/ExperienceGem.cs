using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField, Min(1)] private int experienceValue = 1;

    private bool collected;

    private void OnEnable()
    {
        collected = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !isActiveAndEnabled || Time.timeScale <= 0f) return;
        if (!other.TryGetComponent(out PlayerExperience player) || !player.isActiveAndEnabled) return;

        collected = true;
        player.AddExperience(Mathf.Max(1, experienceValue));
        ObjectPool.Instance.TryReturnToPool(gameObject);
    }
}
