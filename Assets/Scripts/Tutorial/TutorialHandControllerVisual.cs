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
    public float BreathStartedAt { get; private set; }
    private int lastHapticCycle = -1;
    private int lastHapticSlot = int.MinValue;
    private bool appearanceHapticSent;
    private bool secondAppearanceHapticSent;
    private float appearanceHapticAt;
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
    private float completedAt = -1f;
    private bool secondSuccessPulse;
    private float completionHold = 1f;
    public bool IsCompleted => completedAt >= 0f;
    private float CompletionBlend => completedAt < 0f ? 0f : Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - completedAt) / 0.22f);
    public void Complete()
    {
        if (completedAt >= 0f) return;
        completionHold = GetComponent<ControllerGrabPrompt>()?.IsCrawlPrompt == true ? 3f : 1f;
        completedAt = Time.unscaledTime;
        GetComponent<ControllerGrabPrompt>()?.Complete();
        SuccessPulse();
    }
    private void SuccessPulse()
    {
        if (Application.isFocused)
            HapticsUtility.SendHapticImpulse(0.65f, 0.045f,
                hapticHand == XRNode.RightHand ? HapticsUtility.Controller.Right : HapticsUtility.Controller.Left);
    }
    public void ContinueHandReveal()
    {
        ResetBreathing();
        opacity = 1f;
        revealScale = 1f;
        shrinking = false;
        transform.localScale = Vector3.one;
        accentSwapStarted = Time.unscaledTime;
        ApplyOpacity(1f);
    }
    private TutorialPanelDockMotion shrinkMotion;
    public bool IsTracked => tracked && configured && !retiring;
    public float RevealOpacity => TutorialControllerSnapMotion.Smooth(opacity)
        * (completedAt < 0f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - completedAt - 0.22f - completionHold) / 0.28f));

    public void Configure(Transform controller, Transform sceneVisual, float duration,
        float initialScale = 1f, float settleDuration = 1.25f, XRNode hand = XRNode.LeftHand)
    {
        hapticHand = hand;
        ResetBreathing();
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
        ResetBreathing();
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
        ApplyOpacity(RevealOpacity);
        // A distinct, short appearance cue responds as soon as the controller
        // becomes visible. It does not restart or shift the breathing clock.
        if (completedAt < 0f && !appearanceHapticSent && tracked && Application.isFocused
            && RevealOpacity >= 0.05f)
        {
            appearanceHapticSent = true;
            appearanceHapticAt = Time.unscaledTime;
            HapticsUtility.SendHapticImpulse(0.16f, 0.035f,
                hapticHand == XRNode.RightHand ? HapticsUtility.Controller.Right : HapticsUtility.Controller.Left);
        }
        if (completedAt < 0f && appearanceHapticSent && !secondAppearanceHapticSent
            && Time.unscaledTime - appearanceHapticAt >= 0.075f)
        {
            secondAppearanceHapticSent = true;
            if (tracked && Application.isFocused && Time.unscaledTime - appearanceHapticAt < 0.15f)
                HapticsUtility.SendHapticImpulse(0.16f, 0.035f,
                    hapticHand == XRNode.RightHand ? HapticsUtility.Controller.Right : HapticsUtility.Controller.Left);
        }
        if (completedAt < 0f) UpdateBreathHaptics();
        else
        {
            float elapsed = Time.unscaledTime - completedAt;
            if (!secondSuccessPulse && elapsed >= 0.12f) { secondSuccessPulse = true; SuccessPulse(); }
            if (elapsed >= 0.22f + completionHold + 0.28f) { HideAndDestroy(); return; }
        }
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
        if (!tracked || !Application.isFocused || RevealOpacity < 0.05f || breathMaterial == null)
        {
            return;
        }
        // Separate the brisk opening double tap from the breathing rhythm,
        // without shifting the shared visual/haptic phase.
        if (!appearanceHapticSent || Time.unscaledTime - appearanceHapticAt < 0.40f) return;
        // Each presentation starts its own shared visual/haptic clock.
        const float period = 2.4f;
        float cycleTime = (Time.unscaledTime - BreathStartedAt) / period;
        int cycle = Mathf.FloorToInt(cycleTime);
        bool softCycle = (cycle & 1) != 0;
        float phase = cycleTime - cycle;
        // Match the envelope peak to the white coat's cosine maximum (phase 0.5).
        // Start slightly early and extend both releases to shorten the quiet gap.
        // The softer cycle releases sooner, leaving more quiet time.
        const float onsetLead = 0.024f / period;
        const float releaseExtension = 0.102f / period;
        const float start = 0.275f - onsetLead, peak = 0.5f;
        float end = (softCycle ? 0.735f : 0.805f) + releaseExtension;
        float amplitudeScale = softCycle ? 0.8f : 1f;
        if (phase < start) return;
        if (phase > end)
        {
            // Both cycles close at the same absolute phase. The soft cycle has
            // an earlier, lighter beat with the same crisp duration.
            // Drop missed dispatch windows instead of replaying a burst.
            float softEndTime = 0.735f * period + 0.102f;
            float firstBeatTime = softEndTime + 0.10f;
            float finalBeatTime = softEndTime + 0.42f;
            float timeInCycle = phase * period;
            int closingBeat = softCycle && timeInCycle >= firstBeatTime && timeInCycle < firstBeatTime + 0.06f ? 100
                : timeInCycle >= finalBeatTime && timeInCycle < finalBeatTime + 0.06f ? 101 : -1;
            if (closingBeat < 0 || (lastHapticCycle == cycle && lastHapticSlot == closingBeat)) return;
            lastHapticCycle = cycle;
            lastHapticSlot = closingBeat;
            HapticsUtility.SendHapticImpulse(
                breathHapticAmplitude * (softCycle ? (closingBeat == 100 ? 0.55f : 0.65f) : 0.85f) * RevealOpacity,
                0.10f, hapticHand == XRNode.RightHand
                    ? HapticsUtility.Controller.Right : HapticsUtility.Controller.Left);
            return;
        }
        float strength = phase < peak
            ? Mathf.SmoothStep(0f, 1f, (phase - start) / (peak - start))
            : 1f - Mathf.SmoothStep(0f, 1f, (phase - peak) / (end - peak));
        // Let the soft cycle's longer release become progressively more sparse.
        // This changes pulse spacing, not the controller motor's carrier frequency.
        float minPulseRate = softCycle && phase >= peak
            ? breathHapticMinPulseRate * 0.5f : breathHapticMinPulseRate;
        float pulseRate = Mathf.Lerp(minPulseRate, breathHapticMaxPulseRate, strength);
        // Keep the final pulses perceptible so the transition to rest is clear.
        float amplitudeFloor = phase < peak ? breathHapticMinAmplitude : (softCycle ? 0.12f : 0.08f);
        // Analytic integral of the rate curve, anchored at the brightness peak.
        // Integer crossings schedule pulses independently of frame accumulation;
        // slot zero is always the visual maximum. Drop missed slots after stalls.
        bool rising = phase < peak;
        float span = rising ? peak - start : end - peak;
        float u = Mathf.Clamp01((phase - (rising ? start : peak)) / span);
        float integral = u * u * u - 0.5f * u * u * u * u;
        float pulsePosition = rising
            ? period * span * (breathHapticMinPulseRate * (u - 1f)
                + (breathHapticMaxPulseRate - breathHapticMinPulseRate) * (integral - 0.5f))
            : period * span * (breathHapticMaxPulseRate * u
                - (breathHapticMaxPulseRate - minPulseRate) * integral);
        int slot = Mathf.FloorToInt(pulsePosition);
        if (lastHapticCycle == cycle && lastHapticSlot == slot) return;
        lastHapticCycle = cycle;
        lastHapticSlot = slot;
        HapticsUtility.SendHapticImpulse(
            Mathf.Lerp(amplitudeFloor, breathHapticAmplitude, strength) * amplitudeScale * RevealOpacity,
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

    private void ResetBreathing()
    {
        BreathStartedAt = Time.unscaledTime;
        lastHapticCycle = -1;
        lastHapticSlot = int.MinValue;
        appearanceHapticSent = secondAppearanceHapticSent = false;
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
            properties.SetFloat("_BreathTime", Time.unscaledTime - BreathStartedAt);
            properties.SetFloat("_CompletionFrost", CompletionBlend);
            if (bodyMaterial != null)
            {
                properties.SetColor("_UnderlayColor", bodyMaterial.GetColor("_BaseColor"));
                properties.SetFloat("_UnderlayTintStrength", bodyMaterial.GetFloat("_TintStrength"));
                properties.SetFloat("_UnderlayBrightness", bodyMaterial.GetFloat("_SurfaceBrightness"));
                if (completedAt >= 0f) properties.SetFloat("_BlurPixels", bodyMaterial.GetFloat("_BlurPixels"));
            }
            properties.SetFloat("_HintAccentFade", (1f - CompletionBlend) * Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - accentSwapStarted) / 0.25f));
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
                properties.SetFloat("_BreathPeriod", 2.4f);
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
