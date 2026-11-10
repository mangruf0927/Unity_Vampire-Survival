using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed = 5f;
    private float rotationSpeed = 720f;

    private CharacterController characterController;
    private Vector3 moveDirection;
    private float moveSpeedMultiplier = 1f;

    public float MoveSpeed => moveSpeed * moveSpeedMultiplier;

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;

    private void Awake()
    {
        PlayerData data = DataManager.Instance.Players.Get(1);
        moveSpeed = data.moveSpeed;
        rotationSpeed = data.rotationSpeed;

        characterController = GetComponent<CharacterController>();
        Vector3 initialDirection = transform.forward;
        initialDirection.y = 0f;
        LastMoveDirection = initialDirection.sqrMagnitude > 0.000001f ? initialDirection.normalized : Vector3.forward;
    }

    public void OnMove(InputValue value)
    {
        Vector2 moveInput = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
        moveDirection = new(moveInput.x, 0f, moveInput.y);
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        if (moveDirection.sqrMagnitude > 0.000001f) LastMoveDirection = moveDirection.normalized;
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
        characterController.Move(moveDirection * MoveSpeed * Time.deltaTime);
    }

    private void Rotate()
    {
        if (moveDirection.sqrMagnitude == 0f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
