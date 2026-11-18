using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shows or hides the controls panel with a short UI animation.</summary>
public sealed class ControlsPanelToggle : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float animationDuration = 0.22f;
    [SerializeField] private bool startsVisible = true;

    private CanvasGroup canvasGroup;
    private Vector3 shownScale;
    private float animationTime;
    private float animationFrom;
    private float animationTo;
    private bool isVisible;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        shownScale = transform.localScale;
        isVisible = startsVisible;
        animationFrom = animationTo = isVisible ? 1f : 0f;
        Apply(animationTo);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            isVisible = !isVisible;
            animationFrom = canvasGroup.alpha;
            animationTo = isVisible ? 1f : 0f;
            animationTime = 0f;
        }

        if (Mathf.Approximately(canvasGroup.alpha, animationTo))
            return;

        animationTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(animationTime / animationDuration);
        float eased = t * t * (3f - 2f * t);
        Apply(Mathf.Lerp(animationFrom, animationTo, eased));
    }

    private void Apply(float amount)
    {
        canvasGroup.alpha = amount;
        canvasGroup.interactable = amount > 0.99f;
        canvasGroup.blocksRaycasts = amount > 0.99f;
        transform.localScale = shownScale * Mathf.Lerp(0.94f, 1f, amount);
    }
}
