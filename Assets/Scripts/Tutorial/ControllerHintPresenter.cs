using System;
using UnityEngine;

// One owner per hint. Either hand uses the same tracking, reveal, label and glass lifecycle.
public sealed class ControllerHintPresenter : IDisposable
{
    public TutorialHandControllerVisual Model { get; private set; }
    private readonly ControllerHintDefinition definition;
    private IDisposable glass;
    private Camera glassCamera;
    private float lastSeen = float.NegativeInfinity;
    private float nextAttempt;
    public ControllerHintPresenter(ControllerHintDefinition definition) => this.definition = definition;
    public void Update(bool visible, Camera camera, bool continuingHand = false)
    {
        float now = Time.unscaledTime;
        if (visible) lastSeen = now;
        if (visible && Model == null && now >= nextAttempt)
        {
            nextAttempt = now + 0.5f;
            if (definition.controllerPrefab != null
                && TutorialLeftControllerTracking.TryGetSceneVisual(definition.hand, out var controller, out var original))
            {
                var instance = UnityEngine.Object.Instantiate(definition.controllerPrefab, controller, false);
                Model = instance.GetComponent<TutorialHandControllerVisual>();
                Model.Configure(controller, original, definition.revealDuration / 1.2f, 1.5f, definition.shrinkDuration / 1.2f, definition.hand);
                if (continuingHand) Model.ContinueHandReveal();
                instance.AddComponent<ControllerGrabPrompt>().Configure(definition, continuingHand);
            }
        }
        if (!visible && (camera == null || now - lastSeen >= definition.exitDelay)) Hide();
        if (Model != null && camera != null && camera != glassCamera)
        {
            glass?.Dispose();
            glass = TutorialGlassBlur.Acquire(camera);
            glassCamera = camera;
        }
    }
    public void Hide()
    {
        if (Model != null) Model.HideAndDestroy();
        Model = null;
        glass?.Dispose(); glass = null; glassCamera = null;
    }
    public void Dispose() => Hide();
}
