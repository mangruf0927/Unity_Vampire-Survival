using UnityEngine;

public class ExperienceGem : MonoBehaviour
{
    [SerializeField, Min(1)] private int dataId = 1;
    private int experienceValue;

    private bool collected;
    private PlayerExperience attractionTarget;
    private float attractionSpeed;

    private void Awake()
    {
        experienceValue = DataManager.Instance.Gems.Get(dataId).experienceValue;
    }

    private void OnEnable()
    {
        collected = false;
        attractionTarget = null;
        attractionSpeed = 0f;
    }

    public void AttractTo(PlayerExperience player, float speed)
    {
        if (collected || !isActiveAndEnabled || player == null || !player.isActiveAndEnabled) return;
        attractionTarget = player;
        attractionSpeed = Mathf.Max(0f, speed);
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
