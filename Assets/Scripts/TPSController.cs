
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(CharacterController))]
public class TPSController : MonoBehaviour, ITimedStunnable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField, Min(0f)] private float stunDuration = 1.5f;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float animationDampTime = 0.3f;
    [SerializeField, Min(0.1f)] private float basicComboWindow = 0.85f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private CharacterController characterController;
    private Animator animator;
    private InputAction moveAction;
    private float verticalVelocity;
    private float stunRemaining;
    private float animationX;
    private float animationY;
    private float animationXVelocity;
    private float animationYVelocity;
    private int nextBasicComboStep;
    private float basicComboExpiresAt;
    private StunFlipEffect stunFlipEffect;

    public bool IsStunned => stunRemaining > 0f;

    public void PlayDamageAnimation()
    {
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetTrigger("SignalDamage");
    }

    public void PlayParryAnimation()
    {
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetTrigger("signalParry");
    }

    public float PlayBasicAttackAnimation(float fallbackCooldown)
    {
        if (animator == null || !animator.isActiveAndEnabled)
            return fallbackCooldown;

        bool continueCombo = nextBasicComboStep == 2 && Time.time <= basicComboExpiresAt;
        string clipName = continueCombo ? "BasicDamage1" : "BasicDamage0";
        animator.SetTrigger(continueCombo ? "SignalBasicB" : "SignalBasicA");
        nextBasicComboStep = continueCombo ? 1 : 2;
        float animationDuration = GetAnimationClipDuration(clipName, fallbackCooldown);
        float comboWindow = Mathf.Max(basicComboWindow, animationDuration + 0.25f);
        basicComboExpiresAt = Time.time + comboWindow;
        return animationDuration;
    }

    private float GetAnimationClipDuration(string clipName, float fallbackDuration)
    {
        if (animator.runtimeAnimatorController == null)
            return fallbackDuration;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && string.Equals(clips[i].name, clipName, StringComparison.Ordinal))
                return clips[i].length;
        }

        return fallbackDuration;
    }

    public void PlayStunAttackAnimation()
    {
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetTrigger("SignalStun");
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        moveAction = inputActions.FindActionMap("Player")
                                .FindAction("Move");
    }

    private void Start()
    {
        AlignVisualWithCharacterController();
    }

    private void AlignVisualWithCharacterController()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        // Move only the top-level visual child; the controller, camera, and player root stay put.
        Transform visual = renderers[0].transform;
        while (visual.parent != null && visual.parent != transform)
            visual = visual.parent;
        if (visual == transform)
            return;

        Bounds visualBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            visualBounds.Encapsulate(renderers[i].bounds);

        float controllerBottom = transform.position.y + characterController.center.y - characterController.height * 0.5f;
        float verticalOffset = controllerBottom - visualBounds.min.y;
        visual.position += Vector3.up * verticalOffset;
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
        Stun(stunDuration);
    }

    public void Stun(float duration)
    {
        Stun(duration, true);
    }

    public void Stun(float duration, bool flipVisual)
    {
        stunRemaining = Mathf.Max(stunRemaining, duration);
        if (flipVisual)
        {
            if (stunFlipEffect == null)
                stunFlipEffect = GetComponent<StunFlipEffect>() ?? gameObject.AddComponent<StunFlipEffect>();
            stunFlipEffect.Apply(stunRemaining);
        }
        Debug.Log($"{name} está aturdido durante {stunRemaining:F1} s.", this);
    }

    private void MovePlayer()
    {
        Vector2 input = IsStunned || moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();

        // Movimiento en los ejes del mundo: no rota al personaje.
       Vector3 moveDirection = new Vector3(-input.x, 0f, -input.y);

        // Limitar la velocidad diagonal.
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        // The walk Blend Tree uses local movement direction on its x/y axes.
        // Feed zero while stunned or idle so the tree returns to its idle pose.
        if (animator != null)
        {
            float targetX = -moveDirection.x;
            float targetY = moveDirection.z;
            animationX = SmoothAnimationAxis(animationX, targetX, ref animationXVelocity);
            animationY = SmoothAnimationAxis(animationY, targetY, ref animationYVelocity);
            animator.SetFloat("x", animationX);
            animator.SetFloat("y", animationY);
        }

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

    private float SmoothAnimationAxis(float current, float target, ref float velocity)
    {
        current = Mathf.SmoothDamp(current, target, ref velocity, animationDampTime);
        if (Mathf.Approximately(target, 0f) && Mathf.Abs(current) < 0.001f)
        {
            velocity = 0f;
            return 0f;
        }

        return current;
    }
}
