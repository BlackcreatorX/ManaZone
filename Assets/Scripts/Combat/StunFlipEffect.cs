using System.Collections;
using UnityEngine;

public sealed class StunFlipEffect : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform cameraRoot;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.18f;

    private Quaternion originalRotation;
    private Quaternion originalCameraRotation;
    private Vector3 visualPivotLocal;
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

        if (cameraRoot == null)
            cameraRoot = FindCameraRoot();

        if (!hasOriginalRotation)
        {
            CaptureVisualPivot();
            originalRotation = visualRoot.localRotation;
            if (cameraRoot != null)
                originalCameraRotation = cameraRoot.localRotation;
            hasOriginalRotation = true;
        }

        flippedUntil = Mathf.Max(flippedUntil, Time.time + duration);
        if (activeRoutine == null)
            activeRoutine = StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        Quaternion flippedRotation = originalRotation * Quaternion.Euler(0f, 0f, 180f);
        Quaternion flippedCameraRotation = originalCameraRotation * Quaternion.Euler(0f, 0f, 180f);
        yield return RotateTo(flippedRotation, flippedCameraRotation);

        while (Time.time < flippedUntil)
            yield return null;

        while (true)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, transitionDuration);
            Quaternion from = visualRoot.localRotation;
            Quaternion cameraFrom = cameraRoot != null ? cameraRoot.localRotation : Quaternion.identity;
            while (elapsed < duration)
            {
                if (Time.time < flippedUntil)
                    break;

                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetVisualRotation(Quaternion.Slerp(from, originalRotation, t));
                if (cameraRoot != null)
                    cameraRoot.localRotation = Quaternion.Slerp(cameraFrom, originalCameraRotation, t);
                yield return null;
            }

            if (Time.time < flippedUntil)
            {
                yield return RotateTo(flippedRotation, flippedCameraRotation);
                while (Time.time < flippedUntil)
                    yield return null;
                continue;
            }

            break;
        }

        Restore();
    }

    private IEnumerator RotateTo(Quaternion targetRotation, Quaternion targetCameraRotation)
    {
        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;
        Quaternion from = visualRoot.localRotation;
        Quaternion cameraFrom = cameraRoot != null ? cameraRoot.localRotation : Quaternion.identity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetVisualRotation(Quaternion.Slerp(from, targetRotation, t));
            if (cameraRoot != null)
                cameraRoot.localRotation = Quaternion.Slerp(cameraFrom, targetCameraRotation, t);
            yield return null;
        }
        SetVisualRotation(targetRotation);
        if (cameraRoot != null)
            cameraRoot.localRotation = targetCameraRotation;
    }

    private void Restore()
    {
        if (visualRoot != null && hasOriginalRotation)
            SetVisualRotation(originalRotation);
        if (cameraRoot != null && hasOriginalRotation)
            cameraRoot.localRotation = originalCameraRotation;
        hasOriginalRotation = false;
        flippedUntil = 0f;
        activeRoutine = null;
    }

    private Transform FindVisualRoot()
    {
        // Prefer the root that contains the character's renderers. Some player
        // prefabs keep the Animator on the gameplay root, so its transform is
        // not necessarily the model transform that should be flipped.
        Transform bestRoot = null;
        int bestRendererCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            int rendererCount = child.GetComponentsInChildren<Renderer>(true).Length;
            if (rendererCount > bestRendererCount)
            {
                bestRoot = child;
                bestRendererCount = rendererCount;
            }
        }

        if (bestRoot != null)
            return bestRoot;

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.transform != transform)
        {
            Transform candidate = animator.transform;
            while (candidate.parent != null && candidate.parent != transform)
                candidate = candidate.parent;
            if (candidate != transform)
                return candidate;
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
            return null;

        Transform rendererCandidate = renderer.transform;
        while (rendererCandidate.parent != null && rendererCandidate.parent != transform)
            rendererCandidate = rendererCandidate.parent;
        return rendererCandidate;
    }

    private void CaptureVisualPivot()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            visualPivotLocal = Vector3.zero;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        visualPivotLocal = visualRoot.InverseTransformPoint(bounds.center);
    }

    private void SetVisualRotation(Quaternion localRotation)
    {
        Vector3 pivotWorld = visualRoot.TransformPoint(visualPivotLocal);
        visualRoot.localRotation = localRotation;
        visualRoot.position += pivotWorld - visualRoot.TransformPoint(visualPivotLocal);
    }

    private Transform FindCameraRoot()
    {
        Camera playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null && GetComponent<TPSController>() != null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Camera[] sceneCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (sceneCameras.Length > 0)
                    playerCamera = sceneCameras[0];
            }
        }

        if (playerCamera == null || playerCamera.transform == transform)
            return null;

        // If the camera already inherits the visual rotation, rotating it separately
        // would apply the flip twice.
        return visualRoot != null && playerCamera.transform.IsChildOf(visualRoot)
            ? null
            : playerCamera.transform;
    }

    private void OnDisable()
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        Restore();
    }
}
