using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;

    private float moveSpeed;
    private float rotationSpeed;
    private float moveSpeedMultiplier = 1f;
    private Vector3 moveDirection;

    public float MoveSpeed => moveSpeed * moveSpeedMultiplier;
    public Vector3 LastMoveDirection { get; private set; } = Vector3.forward;

    private void Awake()
    {
        PlayerData data = DataManager.Instance.SelectedPlayer;
        moveSpeed = data.moveSpeed;
        rotationSpeed = data.rotationSpeed;

        characterController = GetComponent<CharacterController>();

        Vector3 direction = transform.forward;
        direction.y = 0f;
        LastMoveDirection = direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector3.forward;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f) return;

        Move();
        Rotate();
    }

    private void OnDisable()
    {
        moveDirection = Vector3.zero;
    }

    public void OnMove(InputValue value)
    {
        Vector2 input = Vector2.ClampMagnitude(value.Get<Vector2>(), 1f);
        moveDirection = new Vector3(input.x, 0f, input.y);

        if (moveDirection.sqrMagnitude > 0.000001f)
            LastMoveDirection = moveDirection.normalized;
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, multiplier);
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
