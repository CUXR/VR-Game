using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

// This visual shares the scene controller's tracking and only replaces its appearance.
public sealed class TutorialHandControllerVisual : MonoBehaviour
{
    private Renderer[] renderers;
    private MaterialPropertyBlock properties;
    private float opacity;
    private float fadeDuration;
    private bool configured;
    private Transform originalVisual;
    private Renderer[] originalRenderers;
    private bool[] originalRenderingOff;
    private bool retiring;
    private bool tracked;
    private bool originalHidden;

    public void Configure(Transform controller, Transform sceneVisual, float duration)
    {
        fadeDuration = Mathf.Max(0.01f, duration);
        renderers = GetComponentsInChildren<Renderer>(true);
        properties = new MaterialPropertyBlock();
        // Older generated prefabs may still contain their own pose driver.
        if (TryGetComponent(out TrackedPoseDriver driver)) driver.enabled = false;
        transform.SetParent(controller, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one;
        originalVisual = sceneVisual;
        originalRenderers = sceneVisual.GetComponentsInChildren<Renderer>(true);
        originalRenderingOff = new bool[originalRenderers.Length];
        for (int i = 0; i < originalRenderers.Length; i++)
            originalRenderingOff[i] = originalRenderers[i].forceRenderingOff;
        configured = true;
        HideOriginalVisual();
        ApplyOpacity(0f);
        UpdateVisibility();
    }

    public void HideAndDestroy()
    {
        if (retiring) return;
        retiring = true;
        gameObject.SetActive(false);
        RestoreOriginalVisual();
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        Application.onBeforeRender += UpdateVisibility;
        if (configured && !retiring) HideOriginalVisual();
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateVisibility;
        if (renderers != null)
            foreach (var renderer in renderers)
                if (renderer != null) renderer.enabled = false;
        RestoreOriginalVisual();
    }

    private void LateUpdate()
    {
        if (!configured || retiring) return;
        if (originalVisual == null)
        {
            HideAndDestroy();
            return;
        }
        UpdateVisibility();
        if (!tracked) opacity = 0f;
        else opacity = Mathf.MoveTowards(opacity, 1f, Time.unscaledDeltaTime / fadeDuration);
        ApplyOpacity(TutorialControllerSnapMotion.Smooth(opacity));
    }

    [BeforeRenderOrder(-100)]
    private void UpdateVisibility()
    {
        if (!configured || retiring) return;
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool trackedNow = originalVisual != null && originalVisual.gameObject.activeInHierarchy && left.isValid
            && left.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked
            && left.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state)
            && (state & (InputTrackingState.Position | InputTrackingState.Rotation))
                == (InputTrackingState.Position | InputTrackingState.Rotation);
        // Tracking may arrive after creation. Start the reveal when a real pose
        // is available, so a delayed or recovered device never pops in opaque.
        if (trackedNow && !tracked && !retiring)
        {
            opacity = 0f;
            ApplyOpacity(0f);
        }
        tracked = trackedNow;
        foreach (var renderer in renderers)
            if (renderer != null) renderer.enabled = tracked;
    }

    private void HideOriginalVisual()
    {
        if (originalHidden || originalRenderers == null) return;
        originalHidden = true;
        foreach (var renderer in originalRenderers)
            if (renderer != null) renderer.forceRenderingOff = true;
    }

    private void RestoreOriginalVisual()
    {
        if (!originalHidden) return;
        for (int i = 0; i < originalRenderers.Length; i++)
            if (originalRenderers[i] != null)
                originalRenderers[i].forceRenderingOff = originalRenderingOff[i];
        originalHidden = false;
    }

    private void ApplyOpacity(float alpha)
    {
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(properties);
            properties.SetFloat("_UIFade", alpha);
            renderer.SetPropertyBlock(properties);
        }
    }

    private void OnDestroy()
    {
        RestoreOriginalVisual();
    }
}
