using System.Collections;
using UnityEngine;

public sealed class StunFlipEffect : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.18f;

    private Quaternion originalRotation;
    private Coroutine activeRoutine;
    private bool hasOriginalRotation;
    private float flippedUntil;

    public void Apply(float duration)
    {
        if (duration <= 0f)
            return;

        if (visualRoot == null)
            visualRoot = FindVisualRoot();
        if (visualRoot == null)
            return;

        if (!hasOriginalRotation)
        {
            originalRotation = visualRoot.localRotation;
            hasOriginalRotation = true;
        }

        flippedUntil = Mathf.Max(flippedUntil, Time.time + duration);
        if (activeRoutine == null)
            activeRoutine = StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        Quaternion flippedRotation = originalRotation * Quaternion.Euler(0f, 0f, 180f);
        yield return RotateTo(flippedRotation);

        while (Time.time < flippedUntil)
            yield return null;

        while (true)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, transitionDuration);
            Quaternion from = visualRoot.localRotation;
            while (elapsed < duration)
            {
                if (Time.time < flippedUntil)
                    break;

                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                visualRoot.localRotation = Quaternion.Slerp(from, originalRotation, t);
                yield return null;
            }

            if (Time.time < flippedUntil)
            {
                yield return RotateTo(flippedRotation);
                while (Time.time < flippedUntil)
                    yield return null;
                continue;
            }

            break;
        }

        Restore();
    }

    private IEnumerator RotateTo(Quaternion targetRotation)
    {
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;
        Quaternion from = visualRoot.localRotation;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            visualRoot.localRotation = Quaternion.Slerp(from, targetRotation, t);
            yield return null;
        }
        visualRoot.localRotation = targetRotation;
    }

    private void Restore()
    {
        if (visualRoot != null && hasOriginalRotation)
            visualRoot.localRotation = originalRotation;
        hasOriginalRotation = false;
        flippedUntil = 0f;
        activeRoutine = null;
    }

    private Transform FindVisualRoot()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.transform != transform)
            return animator.transform;

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
            return null;

        Transform candidate = renderer.transform;
        while (candidate.parent != null && candidate.parent != transform)
            candidate = candidate.parent;
        return candidate;
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        Restore();
    }
}
