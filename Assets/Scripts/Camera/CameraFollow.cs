using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 fixedEulerAngles = new(45f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float distance = 20f;

    private void Start()
    {
        if (target == null) return;

        UpdateCameraPose();
    }

    private void LateUpdate()
    {
        if (target == null || Time.deltaTime == 0f) return;

        UpdateCameraPose();
    }

    private void UpdateCameraPose()
    {
        Quaternion rotation = Quaternion.Euler(fixedEulerAngles);

        Vector3 forward = rotation * Vector3.forward;
        Vector3 position = target.position - forward * distance;
        transform.SetPositionAndRotation(position, rotation);
    }
}
