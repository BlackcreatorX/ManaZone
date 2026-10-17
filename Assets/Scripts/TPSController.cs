
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TPSController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private CharacterController characterController;
    private InputAction moveAction;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        moveAction = inputActions.FindActionMap("Player")
                                .FindAction("Move");
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private void Update()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Movimiento en los ejes del mundo: no rota al personaje.
       Vector3 moveDirection = new Vector3(-input.x, 0f, -input.y);

        // Limitar la velocidad diagonal.
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // Gravedad.
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            moveDirection * moveSpeed +
            Vector3.up * verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }
}
