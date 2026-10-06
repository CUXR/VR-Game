using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

// The driver owns the controller pose. This component only adapts the existing
// tracking origin and owns the visual's fade, without modifying the XR rig.
[RequireComponent(typeof(TrackedPoseDriver))]
public sealed class TutorialHandControllerVisual : MonoBehaviour
{
    private Renderer[] renderers;
    private MaterialPropertyBlock properties;
    private float opacity;
    private float fadeDuration;
    private bool configured;
    private Transform ownedOrigin;
    private bool retiring;
    private bool tracked;
    private float fadeOutDuration;

    public void Configure(float duration, float exitDuration = 0.18f)
    {
        fadeDuration = Mathf.Max(0.01f, duration);
        fadeOutDuration = Mathf.Max(0.01f, exitDuration);
        renderers = GetComponentsInChildren<Renderer>(true);
        properties = new MaterialPropertyBlock();
        ownedOrigin = new GameObject("Tutorial controller tracking space").transform;
        transform.SetParent(ownedOrigin, false);
        transform.localScale = Vector3.one;
        configured = true;
        ApplyOpacity(0f);
        UpdateOriginAndVisibility();
    }

    public void HideAndDestroy() { retiring = true; }

    private void OnEnable() { Application.onBeforeRender += UpdateOriginAndVisibility; }
    private void OnDisable() { Application.onBeforeRender -= UpdateOriginAndVisibility; }

    private void LateUpdate()
    {
        if (!configured) return;
        UpdateOriginAndVisibility();
        if (!tracked && !retiring) opacity = 0f;
        else opacity = Mathf.MoveTowards(opacity, retiring ? 0f : 1f,
            Time.unscaledDeltaTime / (retiring ? fadeOutDuration : fadeDuration));
        ApplyOpacity(TutorialControllerSnapMotion.Smooth(opacity));
        if (retiring && opacity <= 0f) Destroy(gameObject);
    }

    [BeforeRenderOrder(-100)]
    private void UpdateOriginAndVisibility()
    {
        if (!configured) return;
        bool originValid = TutorialLeftControllerTracking.TryGetTrackingOrigin(out Pose origin);
        if (originValid) ownedOrigin.SetPositionAndRotation(origin.position, origin.rotation);
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        bool trackedNow = originValid && left.isValid
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
        if (ownedOrigin != null) Destroy(ownedOrigin.gameObject);
    }
}
