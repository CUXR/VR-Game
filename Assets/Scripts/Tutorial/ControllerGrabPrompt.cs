using TMPro;
using UnityEngine;
using System.Collections.Generic;

/// <summary>A view-facing label beside the grip, positioned independently of controller rotation.</summary>
[DefaultExecutionOrder(200)]
public sealed class ControllerGrabPrompt : MonoBehaviour
{
    // Fractions of the controller's physical length; headset distance never scales these.
    [SerializeField] private Vector2 normalOffsetRatio = new Vector2(-0.23f, -0.28f);
    [SerializeField] private float rightOffsetRatio = 0.45f;
    [SerializeField] private float forwardOffsetRatio = 0.04f;
    [SerializeField, Range(0f, 90f)] private float horizontalEnterAngle = 60f;
    [SerializeField, Range(0f, 90f)] private float horizontalExitAngle = 50f;
    private Transform grip;
    private Vector3 modelAxis;
    private Transform label;
    private TextMeshPro prefix;
    private TextMeshPro action;
    private TutorialHandControllerVisual visual;
    private float modelHeight;
    private float labelScale;
    private Vector3 inkCenter;
    private bool horizontal;
    private float horizontalBlend;
    private float blendUpdatedAt;
    private string anchorBone = "left_b_trigger_grip";
    private string prefixText = "PRESS To ";
    private string actionText = "GRAB";
    private bool fadeText;
    private float textAppeared;
    private float completedAt = -1f;
    private bool successText;
    private UnityEngine.XR.XRNode inputHand;
    private bool gripPressed;
    private int attemptFrame = -1;
    private float textSwapAt = -1f;
    private float restoreAt = -1f;
    private bool showingFailure;
    private bool pendingFailure;
    private float baseLabelScale;
    private string targetTag;
    private string completionText = "Well Done!";
    public bool IsCrawlPrompt => actionText == "CRAWL";
    private readonly List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor> grabInteractors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
    public void Complete() { completedAt = Time.unscaledTime; attemptFrame = -1; textSwapAt = restoreAt = -1f; }
    public void Configure(ControllerHintDefinition definition, bool swap = false)
    {
        anchorBone = definition.anchorBone;
        prefixText = definition.prefix;
        actionText = definition.action;
        normalOffsetRatio = definition.normalTextOffset;
        rightOffsetRatio = definition.rightTextOffset;
        forwardOffsetRatio = definition.forwardTextOffset;
        fadeText = swap;
        inputHand = definition.hand;
        targetTag = definition.targetTag;
        completionText = IsCrawlPrompt
            ? "SWING <font=\"Inter-ExtraLight SDF\">Your Arms to </font>MOVE"
            : "<font=\"Inter-ExtraLight SDF\">Well Done!</font>";
    }

    private void Start()
    {
        visual = GetComponent<TutorialHandControllerVisual>();
        if (actionText == "GRAB" && transform.parent != null)
            foreach (var interactor in transform.parent.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))
            {
                grabInteractors.Add(interactor);
                interactor.selectEntered.AddListener(OnGrabbed);
            }
        textAppeared = Time.unscaledTime;
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == anchorBone) grip = child;
        }
        var light = Resources.Load<TMP_FontAsset>("TutorialDesign/Inter-ExtraLight SDF");
        var medium = Resources.Load<TMP_FontAsset>("TutorialDesign/Inter-Medium SDF");
        if (grip == null || light == null || medium == null)
        {
            Debug.LogError("Grab prompt requires the grip bone and both prepared Inter fonts.", this);
            enabled = false;
            return;
        }
        // TMP's font tag switches the connecting words to the same ExtraLight
        // asset used by the prefix, while the action text remains Medium.
        MaterialReferenceManager.AddFontAsset(light);
        MaterialReferenceManager.AddFontAsset(medium);
        modelHeight = MeasureModelHeight();
        label = new GameObject("PRESS To GRAB — Headset Facing").transform;
        label.SetParent(transform, false);
        prefix = CreateText(prefixText, light);
        action = CreateText(actionText, medium);
        // Use the shaped trailing space advance so the two font weights share one line.
        action.transform.localPosition = Vector3.right
            * prefix.textInfo.characterInfo[prefix.textInfo.characterCount - 1].xAdvance;
        Bounds ink = prefix.textBounds;
        Bounds actionInk = action.textBounds;
        actionInk.center += action.transform.localPosition;
        ink.Encapsulate(actionInk);
        // Screenshot: text ink is ~0.70 of the controller's height in width.
        labelScale = modelHeight * 0.70f / Mathf.Max(0.01f, ink.size.x);
        baseLabelScale = labelScale;
        gripPressed = ReadGrip();
        inkCenter = new Vector3(ink.min.x, ink.center.y, 0f);
        label.gameObject.SetActive(false);
    }

    private TextMeshPro CreateText(string content, TMP_FontAsset font)
    {
        var text = new GameObject(content, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        text.transform.SetParent(label, false);
        text.font = font;
        text.text = content;
        text.fontSize = 49.66f;
        text.alignment = TextAlignmentOptions.BaselineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(600f, 100f);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.color = Color.white;
        text.raycastTarget = false;
        text.ForceMeshUpdate();
        return text;
    }

    private void OnEnable() => Application.onBeforeRender += PositionLabel;
    private void OnDisable()
    {
        foreach (var interactor in grabInteractors)
            if (interactor != null) interactor.selectEntered.RemoveListener(OnGrabbed);
        grabInteractors.Clear();
        Application.onBeforeRender -= PositionLabel;
        if (label != null) label.gameObject.SetActive(false);
    }
    private void OnGrabbed(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args)
    {
        if (completedAt < 0f && visual != null && !string.IsNullOrEmpty(targetTag)
            && args.interactableObject.transform.CompareTag(targetTag)) visual.Complete();
    }

    private bool ReadGrip()
    {
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(inputHand);
        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool pressed) && pressed;
    }
    private void LateUpdate()
    {
        bool pressed = ReadGrip();
        if (actionText == "GRAB" && completedAt < 0f && visual != null && visual.IsTracked)
        {
            if (pressed && !gripPressed) attemptFrame = Time.frameCount;
            // XRI has no failed-selection event. Let the next interaction frame
            // settle before testing selection; no fixed wall-clock delay.
            if (attemptFrame >= 0 && Time.frameCount > attemptFrame)
            {
                attemptFrame = -1;
                bool holding = false;
                if (transform.parent != null)
                    foreach (var interactor in transform.parent.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))
                        holding |= interactor.hasSelection;
                if (!holding)
                {
                    if (showingFailure && textSwapAt < 0f) restoreAt = Time.unscaledTime + 1.8f;
                    else { pendingFailure = true; textSwapAt = Time.unscaledTime; }
                }
            }
        }
        gripPressed = pressed;
        PositionLabel();
    }
    private void SetFailureText(bool failure)
    {
        showingFailure = failure;
        prefix.text = failure ? "" : prefixText;
        action.text = failure
            ? "<font=\"Inter-ExtraLight SDF\">Move </font>CLOSER<font=\"Inter-ExtraLight SDF\"> to the Object</font>" : actionText;
        prefix.ForceMeshUpdate();
        action.ForceMeshUpdate();
        action.transform.localPosition = failure ? Vector3.zero : Vector3.right
            * prefix.textInfo.characterInfo[prefix.textInfo.characterCount - 1].xAdvance;
        Bounds ink = action.textBounds;
        ink.center += action.transform.localPosition;
        if (!failure) ink.Encapsulate(prefix.textBounds);
        inkCenter = new Vector3(ink.min.x, ink.center.y, 0f);
        labelScale = failure ? Mathf.Min(baseLabelScale, modelHeight * 1.6f / Mathf.Max(0.01f, ink.size.x)) : baseLabelScale;
    }

    [BeforeRenderOrder(200)]
    private void PositionLabel()
    {
        if (label == null) return;
        float messageOpacity = 1f;
        if (completedAt < 0f)
        {
            if (restoreAt >= 0f && Time.unscaledTime >= restoreAt && textSwapAt < 0f)
            {
                restoreAt = -1f;
                pendingFailure = false;
                textSwapAt = Time.unscaledTime;
            }
            if (textSwapAt >= 0f)
            {
                float elapsed = Time.unscaledTime - textSwapAt;
                if (elapsed >= 0.11f && showingFailure != pendingFailure)
                {
                    SetFailureText(pendingFailure);
                    if (pendingFailure) restoreAt = Time.unscaledTime + 1.8f;
                }
                messageOpacity = elapsed < 0.11f ? 1f - Mathf.SmoothStep(0f, 1f, elapsed / 0.11f)
                    : Mathf.SmoothStep(0f, 1f, (elapsed - 0.11f) / 0.11f);
                if (elapsed >= 0.22f) textSwapAt = -1f;
            }
        }
        var camera = Camera.main;
        bool visible = camera != null && visual != null && visual.IsTracked;
        label.gameObject.SetActive(visible);
        if (!visible) return;
        // Head orientation defines view-right; head position only turns the text
        // face. Moving closer cannot change the placement basis or spacing.
        Vector3 forward = camera.transform.forward;
        Vector3 right = camera.transform.right;
        Vector3 up = camera.transform.up;
        float scale = Mathf.Abs(transform.lossyScale.x);
        Vector3 anchor = grip.position + right * (modelHeight * rightOffsetRatio * scale);
        Vector3 axis = transform.TransformDirection(modelAxis);
        float horizontalAmount = Mathf.Abs(Vector3.Dot(axis, right));
        float verticalAmount = Mathf.Abs(Vector3.Dot(axis, up));
        if (horizontalAmount + verticalAmount > 0.0001f)
        {
            float angle = Mathf.Atan2(horizontalAmount, verticalAmount) * Mathf.Rad2Deg;
            horizontal = horizontal ? angle > horizontalExitAngle : angle >= horizontalEnterAngle;
        }
        float now = Time.unscaledTime;
        // Called in LateUpdate and before render: elapsed time prevents a double-speed blend.
        float delta = Mathf.Max(0f, now - blendUpdatedAt);
        blendUpdatedAt = now;
        horizontalBlend = Mathf.Lerp(horizontalBlend, horizontal ? 1f : 0f, 1f - Mathf.Exp(-delta / 0.1f));
        float length = modelHeight * scale;
        Vector3 viewOffset = new Vector3(normalOffsetRatio.x * (1f - horizontalBlend),
            normalOffsetRatio.y * (1f - horizontalBlend), -forwardOffsetRatio) * length;
        anchor += right * viewOffset.x + up * viewOffset.y + forward * viewOffset.z;
        label.rotation = Quaternion.LookRotation(anchor - camera.transform.position, up);
        label.localScale = Vector3.one * labelScale;
        label.position = anchor - label.TransformVector(inkCenter);
        float opacity = visual.RevealOpacity * messageOpacity;
        if (completedAt >= 0f)
        {
            float elapsed = Time.unscaledTime - completedAt;
            if (!successText && elapsed >= 0.11f)
            {
                successText = true;
                prefix.text = IsCrawlPrompt ? "Now " : "";
                action.text = completionText;
                prefix.ForceMeshUpdate();
                action.transform.localPosition = IsCrawlPrompt ? Vector3.right
                    * prefix.textInfo.characterInfo[prefix.textInfo.characterCount - 1].xAdvance : Vector3.zero;
                action.ForceMeshUpdate();
                Bounds completionInk = action.textBounds;
                completionInk.center += action.transform.localPosition;
                if (IsCrawlPrompt) completionInk.Encapsulate(prefix.textBounds);
                labelScale = IsCrawlPrompt
                    ? Mathf.Min(baseLabelScale, modelHeight * 1.6f / Mathf.Max(0.01f, completionInk.size.x)) : baseLabelScale;
                inkCenter = new Vector3(completionInk.min.x, completionInk.center.y, 0f);
            }
            opacity *= successText ? Mathf.SmoothStep(0f, 1f, (elapsed - 0.11f) / 0.11f)
                : 1f - Mathf.SmoothStep(0f, 1f, elapsed / 0.11f);
        }
        if (fadeText) opacity *= Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - textAppeared) / 0.25f);
        prefix.alpha = opacity;
        action.alpha = opacity;
    }

    private float MeasureModelHeight()
    {
        var points = new List<Vector3>();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.gameObject.activeInHierarchy) continue;
            Mesh baked = null;
            Mesh mesh = null;
            float unitCorrection = 1f;
            if (renderer is SkinnedMeshRenderer skin)
            {
                // Generated skinned assets are not CPU-readable in builds. Bake
                // once, then normalize the FBX unit conversion to authored bounds.
                baked = new Mesh();
                skin.BakeMesh(baked);
                baked.RecalculateBounds();
                mesh = baked;
                unitCorrection = skin.sharedMesh.bounds.size.magnitude
                    / Mathf.Max(0.000001f, baked.bounds.size.magnitude);
            }
            else if (renderer.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
            if (mesh != null)
                foreach (Vector3 vertex in mesh.vertices)
                    points.Add(transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex * unitCorrection)));
            if (baked != null)
            {
                if (Application.isPlaying) Destroy(baked);
                else DestroyImmediate(baked);
            }
        }
        if (points.Count == 0) { modelAxis = Vector3.up; return 0.15f; }
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in points) center += point;
        center /= points.Count;
        // Principal direction of the actual controller geometry, not its buttons or pivot axes.
        float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach (Vector3 point in points)
        {
            Vector3 p = point - center;
            xx += p.x * p.x; xy += p.x * p.y; xz += p.x * p.z;
            yy += p.y * p.y; yz += p.y * p.z; zz += p.z * p.z;
        }
        modelAxis = Vector3.one.normalized;
        for (int i = 0; i < 24; i++)
            modelAxis = new Vector3(xx * modelAxis.x + xy * modelAxis.y + xz * modelAxis.z,
                xy * modelAxis.x + yy * modelAxis.y + yz * modelAxis.z,
                xz * modelAxis.x + yz * modelAxis.y + zz * modelAxis.z).normalized;
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        foreach (Vector3 point in points)
        {
            float distance = Vector3.Dot(point, modelAxis);
            min = Mathf.Min(min, distance); max = Mathf.Max(max, distance);
        }
        return Mathf.Max(0.01f, max - min);
    }
}
