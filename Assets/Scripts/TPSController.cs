
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TPSController : MonoBehaviour, IStunnable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(0f)] private float stunDuration = 1.5f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private CharacterController characterController;
    private InputAction moveAction;
    private float verticalVelocity;
    private float stunRemaining;

    public bool IsStunned => stunRemaining > 0f;

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
        if (stunRemaining > 0f)
            stunRemaining = Mathf.Max(0f, stunRemaining - Time.deltaTime);
        MovePlayer();
    }

    public void Stun()
    {
        stunRemaining = Mathf.Max(stunRemaining, stunDuration);
        Debug.Log($"{name} está aturdido durante {stunDuration:F1} s.", this);
    }

    private void MovePlayer()
    {
        Vector2 input = IsStunned || moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();

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
