using UnityEngine;
using System.Collections.Generic;

/// <summary>Show the tracked grip tutorial while any Wire is in the headset view.</summary>
[DisallowMultipleComponent]
public sealed class ControllerOnlyDisplay : MonoBehaviour
{
    [SerializeField] private GameObject controllerPrefab;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.65f;
    [SerializeField, Min(0.1f)] private float shrinkDuration = 1.25f;
    [SerializeField, Min(0f)] private float viewExitDelay = 0.2f;
    [SerializeField, Min(0.05f)] private float wireReachDistance = 2.5f;
    private readonly List<Transform> visibleWires = new List<Transform>();
    private readonly List<WireAttentionVisual> wireEffects = new List<WireAttentionVisual>();
    private readonly WireViewVisibility wireView = new WireViewVisibility();
    private float lastWireSeen = float.NegativeInfinity;
    private float nextShowAttempt;
    private TutorialHandControllerVisual model;
    private Camera blurCamera;
    private System.IDisposable modelGlass;

    private void OnEnable()
    {
        wireView.Invalidate();
        lastWireSeen = float.NegativeInfinity;
        nextShowAttempt = 0f;
    }

    private void Update()
    {
        var view = Camera.main;
        visibleWires.Clear();
        bool visible = view != null && view.isActiveAndEnabled
            && wireView.CollectVisible(view, wireReachDistance, visibleWires);
        foreach (var wire in visibleWires)
        {
            var effect = wire.GetComponent<WireAttentionVisual>();
            if (effect == null) effect = wire.gameObject.AddComponent<WireAttentionVisual>();
            if (!wireEffects.Contains(effect)) wireEffects.Add(effect);
        }
        for (int i = wireEffects.Count - 1; i >= 0; i--)
        {
            var effect = wireEffects[i];
            if (effect == null) { wireEffects.RemoveAt(i); continue; }
            effect.SetVisible(visibleWires.Contains(effect.transform), view);
        }
        float now = Time.unscaledTime;
        if (visible) lastWireSeen = now;
        if (visible && model == null && now >= nextShowAttempt)
        {
            nextShowAttempt = now + 0.5f;
            ShowReplacement();
        }
        else if (!visible && model != null && (view == null || now - lastWireSeen >= viewExitDelay))
            RestoreOriginal();
    }

    private void ShowReplacement()
    {
        if (controllerPrefab == null
            || !controllerPrefab.TryGetComponent<TutorialHandControllerVisual>(out _))
            return;
        if (!TutorialLeftControllerTracking.TryGetSceneVisual(out Transform controller, out Transform visual))
        {
            Debug.LogWarning("Cannot replace the left controller: its scene visual is unavailable.", this);
            return;
        }
        var instance = Instantiate(controllerPrefab, controller, false);
        model = instance.GetComponent<TutorialHandControllerVisual>();
        const float animationSpeed = 1.2f;
        model.Configure(controller, visual, revealDuration / animationSpeed,
            1.5f, shrinkDuration / animationSpeed);
        instance.AddComponent<ControllerGrabPrompt>();
    }

    private void LateUpdate()
    {
        Camera view = model != null ? Camera.main : null;
        if (view == null || !view.isActiveAndEnabled)
        {
            ReleaseGlass();
            return;
        }
        if (blurCamera == view && modelGlass != null) return;
        ReleaseGlass();
        blurCamera = view;
        modelGlass = TutorialGlassBlur.Acquire(view);
    }

    private void RestoreOriginal()
    {
        if (model != null) model.HideAndDestroy();
        model = null;
        ReleaseGlass();
    }

    private void ReleaseGlass()
    {
        modelGlass?.Dispose();
        modelGlass = null;
        blurCamera = null;
    }

    private void OnDisable()
    {
        RestoreOriginal();
        foreach (var effect in wireEffects)
            if (effect != null) { effect.enabled = false; Destroy(effect); }
        wireEffects.Clear();
    }

    private void OnDestroy()
    {
        RestoreOriginal();
    }
}
