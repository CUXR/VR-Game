using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class TutorialControllerModelDisplay : MonoBehaviour
{
    [SerializeField] private float rotationAmplitude = 15f;
    [SerializeField] private float rotationPeriod = 4f;
    private float elapsed;
    private Quaternion referenceOrientation;
    private TutorialPanelStyle style;
    private Transform presentationParent;
    private Vector3 presentationPosition;
    private Vector3 presentationScale;
    private Transform visual;
    private Renderer[] renderers;
    private MaterialPropertyBlock properties;
    private float panelOpacity;
    private bool gripHeld;
    private bool flying;
    private bool handedOff;
    private float flightStartedAt;
    private Pose startPose;
    private Pose capturedHandPose;
    private Quaternion arrivalRotation;
    private Vector3 startScale;
    private Vector3 arrivalScale;
    private TutorialHandControllerVisual handVisual;
    private Transform sceneController;
    private Transform sceneVisual;
    private float presentationFade = 1f;

    public event System.Action<bool> HandModeChanged;

    public bool HasHandVisual => handVisual != null;

    public void Configure(TutorialPanelStyle style, Vector3 illustrationCenter)
    {
        this.style = style;
        // The prefab is normalized to one metre high. Its parent uses Figma pixel units.
        transform.localScale = Vector3.one * style.panelHeight * style.controllerHeightRatio;
        float forward = Mathf.Sqrt(style.headsetDistance * style.headsetDistance
            - style.verticalOffset * style.verticalOffset);
        Vector3 panelOrigin = new Vector3(0f, style.verticalOffset, forward);
        Vector3 illustrationInView = panelOrigin + illustrationCenter * style.canvasScale;
        // Move along the illustration's sightline, keeping its apparent centre aligned
        // when the model is closer to the headset than the curved panel artwork.
        Vector3 modelInView = illustrationInView - illustrationInView.normalized
            * (style.controllerForwardOffset - style.foregroundDepth);
        transform.localPosition = (modelInView - panelOrigin) / style.canvasScale
            + Vector3.right * (style.controllerHorizontalOffset / style.canvasScale);
        referenceOrientation = Quaternion.LookRotation(modelInView.normalized, Vector3.up);
        transform.localRotation = referenceOrientation;
        rotationAmplitude = style.controllerRotationAmplitude;
        rotationPeriod = Mathf.Max(0.1f, style.controllerRotationPeriod);
        elapsed = 0f;
        presentationParent = transform.parent;
        presentationPosition = transform.localPosition;
        presentationScale = transform.localScale;
        visual = transform.GetChild(0);
        renderers = GetComponentsInChildren<Renderer>(true);
        properties = new MaterialPropertyBlock();
        gripHeld = TutorialLeftControllerTracking.Grip() >= 0.75f;
    }

    private void OnEnable()
    {
        // Opening the UI while already squeezing is not a new press.
        gripHeld = TutorialLeftControllerTracking.Grip() >= 0.75f;
    }

    public void SetPanelOpacity(float alpha)
    {
        panelOpacity = alpha;
        if ((flying || handedOff) && alpha <= 0f) RestorePresentation();
    }

    private void Update()
    {
        if (style == null) return;
        bool pressed = TutorialControllerSnapMotion.GripPress(TutorialLeftControllerTracking.Grip(), ref gripHeld);
        if (pressed && (handedOff || panelOpacity > 0.1f))
        {
            if (flying || handedOff) RestorePresentation();
            else if (TutorialLeftControllerTracking.TryGetWorldPose(out Pose handPose))
                BeginFlight(handPose);
            else
                Debug.LogWarning("Tutorial grip pressed, but the left controller pose is not tracked yet.", this);
        }
        if (flying || handedOff) return;
        elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, rotationPeriod);
        float yaw = rotationAmplitude * Mathf.Sin(elapsed * 2f * Mathf.PI / rotationPeriod);
        transform.localRotation = Quaternion.AngleAxis(yaw, Vector3.up) * referenceOrientation;
    }

    private void BeginFlight(Pose handPose)
    {
        if (style.handControllerPrefab == null)
        {
            Debug.LogError("Tutorial tracked controller prefab is missing. Rebuild the tutorial controller assets.", this);
            return;
        }
        if (!TutorialLeftControllerTracking.TryGetSceneVisual(out sceneController, out sceneVisual)
            || style.handControllerPrefab.GetComponent<TutorialHandControllerVisual>() == null)
        {
            Debug.LogWarning("Tutorial handoff needs the scene's Left Controller Visual and a valid tutorial hand prefab.", this);
            return;
        }
        capturedHandPose = handPose;
        startPose = new Pose(transform.position, transform.rotation);
        startScale = transform.lossyScale;
        // The child stores only the large UI presentation pose. Undo that pose
        // at arrival so the geometry reaches the real controller orientation.
        arrivalRotation = handPose.rotation * Quaternion.Inverse(visual.localRotation);
        arrivalScale = sceneController.lossyScale / Mathf.Max(0.00001f, visual.localScale.x);
        flightStartedAt = Time.unscaledTime;
        flying = true;
        // Freeze the flight in world space; head movement must not drag its path.
        transform.SetParent(null, true);
        HandModeChanged?.Invoke(true);
    }

    private void LateUpdate()
    {
        if (style == null) return;
        if ((flying || handedOff) && (sceneController == null || sceneVisual == null
            || !sceneController.gameObject.activeInHierarchy || (handedOff && handVisual == null)))
        {
            RestorePresentation();
            return;
        }
        if (handedOff) return;
        presentationFade = Mathf.MoveTowards(presentationFade, 1f,
            Time.unscaledDeltaTime / Mathf.Max(0.01f, style.controllerDisplayFadeDuration));
        float alpha = 1f;
        if (flying)
        {
            capturedHandPose = new Pose(sceneController.position, sceneController.rotation);
            arrivalRotation = capturedHandPose.rotation * Quaternion.Inverse(visual.localRotation);
            arrivalScale = sceneController.lossyScale / Mathf.Max(0.00001f, visual.localScale.x);
            float progress = Mathf.Clamp01((Time.unscaledTime - flightStartedAt)
                / Mathf.Max(0.1f, style.controllerSnapDuration));
            float travel = TutorialControllerSnapMotion.Travel(progress);
            float shrink = TutorialControllerSnapMotion.Smooth(progress);
            transform.SetPositionAndRotation(Vector3.Lerp(startPose.position, capturedHandPose.position, travel),
                Quaternion.Slerp(startPose.rotation, arrivalRotation, shrink));
            transform.localScale = Vector3.Lerp(startScale, arrivalScale, shrink);
            alpha = TutorialControllerSnapMotion.DisplayOpacity(
                Vector3.Distance(transform.position, capturedHandPose.position),
                style.controllerSnapFadeDistance, style.controllerSnapFadeEndDistance);
            // Begin the real controller's slow reveal as the flying display
            // becomes fully transparent, without waiting for its hidden arrival.
            if (alpha <= 0f) ShowHandVisual();
            if (handVisual != null) alpha = 0f;
            if (progress >= 1f)
            {
                CompleteHandoff();
                return;
            }
        }
        ApplyOpacity(panelOpacity * alpha * TutorialControllerSnapMotion.Smooth(presentationFade));
    }

    private void CompleteHandoff()
    {
        ApplyOpacity(0f);
        ShowHandVisual();
        flying = false;
        handedOff = true;
        // Keep the input owner active while the large presentation is hidden.
        transform.SetParent(presentationParent.parent, false);
        transform.localPosition = presentationPosition;
        transform.localRotation = referenceOrientation;
        transform.localScale = presentationScale;
        visual.gameObject.SetActive(false);
    }

    private void ShowHandVisual()
    {
        if (handVisual != null) return;
        // The authored grip pose shares the original controller's tracking and scale.
        var handRoot = Instantiate(style.handControllerPrefab, sceneController, false);
        handVisual = handRoot.GetComponent<TutorialHandControllerVisual>();
        handVisual.Configure(sceneController, sceneVisual, style.controllerHandFadeDuration);
    }

    private void RestorePresentation()
    {
        flying = false;
        handedOff = false;
        sceneController = null;
        sceneVisual = null;
        if (handVisual != null)
        {
            handVisual.HideAndDestroy();
            handVisual = null;
        }
        transform.SetParent(presentationParent, false);
        transform.localPosition = presentationPosition;
        transform.localRotation = referenceOrientation;
        transform.localScale = presentationScale;
        visual.gameObject.SetActive(true);
        elapsed = 0f;
        presentationFade = 0f;
        ApplyOpacity(0f);
        HandModeChanged?.Invoke(false);
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
        if (handVisual != null) handVisual.HideAndDestroy();
    }

    private void OnDisable()
    {
        if (presentationParent != null && (flying || handedOff)) RestorePresentation();
    }
}
