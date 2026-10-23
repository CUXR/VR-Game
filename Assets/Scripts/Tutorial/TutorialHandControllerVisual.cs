using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

// This visual shares the scene controller's tracking and only replaces its appearance.
public sealed class TutorialHandControllerVisual : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float breathHapticAmplitude = 0.55f;
    [SerializeField, Range(0f, 1f)] private float breathHapticMinAmplitude = 0.04f;
    [SerializeField, Min(0.1f)] private float breathHapticMinPulseRate = 2f;
    [SerializeField, Min(0.1f)] private float breathHapticMaxPulseRate = 14f;
    [SerializeField, Min(0.01f)] private float breathHapticDuration = 0.08f;
    private XRNode hapticHand;
    private Material breathMaterial;
    private float hapticPulsePhase;
    private Renderer[] renderers;
    private Material[][] rendererMaterials;
    private Material bodyMaterial;
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
    private float revealScale = 1f;
    private float shrinkDuration;
    private bool shrinking;
    private float accentSwapStarted = -1000f;
    public void ContinueHandReveal()
    {
        opacity = 1f;
        revealScale = 1f;
        shrinking = false;
        transform.localScale = Vector3.one;
        accentSwapStarted = Time.unscaledTime;
        ApplyOpacity(1f);
    }
    private TutorialPanelDockMotion shrinkMotion;
    public bool IsTracked => tracked && configured && !retiring;
    public float RevealOpacity => TutorialControllerSnapMotion.Smooth(opacity);

    public void Configure(Transform controller, Transform sceneVisual, float duration,
        float initialScale = 1f, float settleDuration = 1.25f, XRNode hand = XRNode.LeftHand)
    {
        hapticHand = hand;
        hapticPulsePhase = 0f;
        fadeDuration = Mathf.Max(0.01f, duration);
        revealScale = Mathf.Max(1f, initialScale);
        shrinkDuration = Mathf.Max(0.1f, settleDuration);
        ResetReveal();
        renderers = GetComponentsInChildren<Renderer>(true);
        rendererMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            rendererMaterials[i] = renderers[i].sharedMaterials;
            foreach (var material in rendererMaterials[i])
            {
                if (material != null && material.name.StartsWith("Controller Body Transparent")) bodyMaterial = material;
                if (material != null && material.name.StartsWith("Controller Side Grip White")) breathMaterial = material;
            }
        }
        properties = new MaterialPropertyBlock();
        transform.SetParent(controller, false);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        transform.localScale = Vector3.one * revealScale;
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
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }

    private void OnEnable()
    {
        Application.onBeforeRender += UpdateVisibility;
        if (configured && !retiring) HideOriginalVisual();
    }

    private void OnDisable()
    {
        hapticPulsePhase = 0f;
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
        if (tracked) opacity = Mathf.MoveTowards(opacity, 1f, Time.unscaledDeltaTime / fadeDuration);
        ApplyOpacity(TutorialControllerSnapMotion.Smooth(opacity));
        UpdateBreathHaptics();
        // Reveal and shrink together. Keep the original curve's rebound,
        // but apply it only to scale, never to the tracked rotation.
        if (tracked && !shrinking)
        {
            shrinking = true;
            shrinkMotion.Retarget(1f, shrinkDuration, Time.unscaledTimeAsDouble);
        }
        if (shrinking)
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(revealScale, 1f,
                shrinkMotion.Evaluate(Time.unscaledTimeAsDouble).value);
    }

    private void UpdateBreathHaptics()
    {
        if (!tracked || !Application.isFocused || RevealOpacity < 0.1f || breathMaterial == null)
        {
            hapticPulsePhase = 0f;
            return;
        }
        // Match the white coat's shader clock, cosine curve and opacity range.
        float period = Mathf.Max(0.1f, breathMaterial.GetFloat("_BreathPeriod"));
        float breath = 0.5f - 0.5f * Mathf.Cos(Time.realtimeSinceStartup * 2f * Mathf.PI / period);
        float whiteOpacity = Mathf.Lerp(breathMaterial.GetFloat("_BreathOpacityMin"),
            breathMaterial.GetFloat("_BreathOpacityMax"), breath);
        float strength = Mathf.InverseLerp(
            Mathf.Min(breathMaterial.GetFloat("_BreathOpacityMin"), breathMaterial.GetFloat("_BreathOpacityMax")),
            Mathf.Max(breathMaterial.GetFloat("_BreathOpacityMin"), breathMaterial.GetFloat("_BreathOpacityMax")), whiteOpacity);
        float pulseRate = Mathf.Lerp(breathHapticMinPulseRate, breathHapticMaxPulseRate, strength);
        // Integrate the changing rate rather than rescheduling a fixed timer.
        // Drop missed pulses after a stall so they never arrive in a burst.
        hapticPulsePhase += Mathf.Min(Time.unscaledDeltaTime, 0.1f) * Mathf.Max(0.1f, pulseRate);
        if (hapticPulsePhase < 1f) return;
        hapticPulsePhase %= 1f;
        HapticsUtility.SendHapticImpulse(
            Mathf.Lerp(breathHapticMinAmplitude, breathHapticAmplitude, strength) * RevealOpacity,
            Mathf.Min(breathHapticDuration, 0.7f / Mathf.Max(0.1f, pulseRate)),
            hapticHand == XRNode.RightHand ? HapticsUtility.Controller.Right : HapticsUtility.Controller.Left);
    }

    [BeforeRenderOrder(-100)]
    private void UpdateVisibility()
    {
        if (!configured || retiring) return;
        // The scene controller owns tracking and retains its last pose during
        // brief tracking loss. A second device poll was hiding the replacement
        // and replaying its reveal whenever tracking flags flickered.
        // Reveal/scale are initialized only when a new tutorial visual is created.
        tracked = originalVisual != null && transform.parent != null
            && transform.parent.gameObject.activeInHierarchy;
        foreach (var renderer in renderers)
            if (renderer != null) renderer.enabled = tracked;
    }

    private void ResetReveal()
    {
        opacity = 0f;
        shrinking = false;
        shrinkMotion = new TutorialPanelDockMotion();
        transform.localScale = Vector3.one * revealScale;
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
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            var renderer = renderers[rendererIndex];
            if (renderer == null) continue;
            renderer.GetPropertyBlock(properties);
            properties.SetFloat("_UIFade", alpha);
            properties.SetFloat("_BreathTime", Time.realtimeSinceStartup);
            properties.SetFloat("_HintAccentFade", Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - accentSwapStarted) / 0.25f));
            properties.SetFloat("_WhiteFrostedSurface", 0f);
            properties.SetFloat("_BreathOpacityMin", 1f);
            properties.SetFloat("_BreathOpacityMax", 1f);
            renderer.SetPropertyBlock(properties);
            // Apply explicitly to the white submesh too: per-material property blocks
            // otherwise take precedence over the renderer-level animation values.
            var materials = rendererMaterials[rendererIndex];
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || !material.name.StartsWith("Controller Side Grip White")) continue;
                properties.SetFloat("_WhiteFrostedSurface", 1f);
                properties.SetFloat("_BreathOpacityMin", material.GetFloat("_BreathOpacityMin"));
                properties.SetFloat("_BreathOpacityMax", material.GetFloat("_BreathOpacityMax"));
                properties.SetFloat("_BreathPeriod", material.GetFloat("_BreathPeriod"));
                if (bodyMaterial != null)
                {
                    properties.SetColor("_UnderlayColor", bodyMaterial.GetColor("_BaseColor"));
                    properties.SetFloat("_UnderlayTintStrength", bodyMaterial.GetFloat("_TintStrength"));
                    properties.SetFloat("_UnderlayBrightness", bodyMaterial.GetFloat("_SurfaceBrightness"));
                    properties.SetFloat("_BlurPixels", bodyMaterial.GetFloat("_BlurPixels"));
                    properties.SetFloat("_RimStrength", bodyMaterial.GetFloat("_RimStrength"));
                    properties.SetFloat("_RimSoftStrength", bodyMaterial.GetFloat("_RimSoftStrength"));
                    properties.SetFloat("_RimPower", bodyMaterial.GetFloat("_RimPower"));
                    properties.SetFloat("_BoundaryLineStrength", bodyMaterial.GetFloat("_BoundaryLineStrength"));
                    properties.SetFloat("_BoundaryGlowStrength", bodyMaterial.GetFloat("_BoundaryGlowStrength"));
                    properties.SetFloat("_BoundaryWidth", bodyMaterial.GetFloat("_BoundaryWidth"));
                    properties.SetFloat("_BoundarySoftness", bodyMaterial.GetFloat("_BoundarySoftness"));
                    properties.SetFloat("_ButtonSoftness", bodyMaterial.GetFloat("_ButtonSoftness"));
                    properties.SetTexture("_EdgeMask", bodyMaterial.GetTexture("_EdgeMask"));
                    properties.SetFloat("_ButtonBoundaryMultiplier", 1f);
                }
                renderer.SetPropertyBlock(properties, i);
                // Clear slot-only overrides before reusing the block for another renderer.
                renderer.GetPropertyBlock(properties);
            }
        }
    }

    private void OnDestroy()
    {
        RestoreOriginalVisual();
    }
}
