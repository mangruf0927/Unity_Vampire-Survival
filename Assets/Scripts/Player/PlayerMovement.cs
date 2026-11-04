using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 720f;

    private CharacterController characterController;
    private Vector3 moveDirection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void OnMove(InputValue value)
    {
        Vector2 moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
        moveDirection = new(moveInput.x, 0f, moveInput.y);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
    }

    private void Update()
    {
        if (Time.deltaTime == 0f) return;

        Move();
        Rotate();
    }

    private void OnDisable()
    {
        moveDirection = Vector2.zero;
    }

    private void Move()
    {
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
    }

    private void Rotate()
    {
        if (moveDirection.sqrMagnitude == 0f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
