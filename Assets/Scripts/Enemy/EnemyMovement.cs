using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float rotationSpeed = 540f;
    [SerializeField, Min(0f)] private float stoppingDistance;

    private void Update()
    {
        if (target == null || Time.deltaTime == 0f) return;

        Vector3 targetPosition = new(target.position.x, transform.position.y, target.position.z);

        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;

        if (distance <= stoppingDistance) return;

        Move(targetPosition, distance);
        Rotate(direction);
    }

    private void Move(Vector3 targetPosition, float distance)
    {
        float moveDistance = Mathf.Min(moveSpeed * Time.deltaTime, distance - stoppingDistance);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveDistance);
    }

    private void Rotate(Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
