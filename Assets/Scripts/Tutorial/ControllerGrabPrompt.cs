using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>A headset-following tutorial label with a leader to the relevant button.</summary>
[DefaultExecutionOrder(200)]
public sealed class ControllerGrabPrompt : MonoBehaviour
{
    private const float FollowDistance = 0.9f;
    private const float FollowHeight = 0.02f;
    private const float BarHeight = 0.044f;
    private const float StubLength = 0.020f;
    private const float LineWidth = 0.004f;
    private const float TextGap = 0.012f;
    // The short button-side line runs level or upright from the button to just
    // past the controller's outline, so the diagonal never crosses the controller.
    private const float ButtonStubMargin = 0.006f;
    private const float ButtonStubMinLength = 0.015f;
    private Transform grip;
    // Lit button triangles (three corners each) and their outward normals,
    // stored in the animated button bone's space.
    private Vector3[] buttonTriangles;
    private Vector3[] buttonNormals;
    // Whole controller shell in this object's local space, for its outline.
    private Vector3[] shellVertices;
    private int[] shellTriangles;
    private Vector2[] projectedShell;
    private LazyFollow follow;
    private Transform label;
    private Mesh leaderMesh;
    private MeshRenderer leaderRenderer;
    private Material leaderMaterial;
    private readonly Vector3[] leaderVertices = new Vector3[16];
    private float textLeft;
    private TextMeshPro prefix;
    private TextMeshPro action;
    private TutorialHandControllerVisual visual;
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
    private string targetTag;
    private string completionText = "WELL DONE! You've got it!";
    public bool IsCrawlPrompt => actionText == "CRAWL";
    private readonly List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor> grabInteractors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>();
    public void Complete() { completedAt = Time.unscaledTime; attemptFrame = -1; textSwapAt = restoreAt = -1f; }
    public void Configure(ControllerHintDefinition definition, bool swap = false)
    {
        anchorBone = definition.anchorBone;
        prefixText = definition.prefix;
        actionText = definition.action;
        fadeText = swap;
        inputHand = definition.hand;
        targetTag = definition.targetTag;
        completionText = IsCrawlPrompt
            ? "PADDLE <font=\"Inter-ExtraLight SDF\">Your Forearms Up and Down to Crawl</font>"
            : "WELL DONE! <font=\"Inter-ExtraLight SDF\">You've got it!</font>";
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
        var root = new GameObject("Tutorial Prompt — Headset Follow");
        root.SetActive(false);
        follow = root.AddComponent<LazyFollow>();
        follow.targetOffset = new Vector3(0f, FollowHeight, FollowDistance);
        follow.rotationFollowMode = LazyFollow.RotationFollowMode.LookAt;
        follow.movementSpeed = 8f;
        label = new GameObject("Tutorial Text").transform;
        label.SetParent(root.transform, false);
        prefix = CreateText(FormatPrefix(prefixText), light);
        action = CreateText(actionText, medium);
        LayoutText();
        CacheButtonSurface();
        CreateLeader(root.transform);
        gripPressed = ReadGrip();
    }

    // Tutorial typography: uppercase words use Medium; sentence-case words use ExtraLight.
    private static string FormatPrefix(string text) => System.Text.RegularExpressions.Regex.Replace(
        text, @"\b[A-Z]+\b", match => "<font=\"Inter-Medium SDF\">" + match.Value + "</font>");

    private TextMeshPro CreateText(string content, TMP_FontAsset font)
    {
        var text = new GameObject(content, typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        text.transform.SetParent(label, false);
        text.font = font;
        // Apply casing after rich-text parsing so emphasis and font names survive.
        text.fontStyle |= FontStyles.UpperCase;
        text.text = content;
        text.fontSize = 49.66f;
        text.alignment = TextAlignmentOptions.BaselineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(600f, 100f);
        text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.color = Color.white;
        text.raycastTarget = false;
        text.ForceMeshUpdate(true);
        return text;
    }

    private void OnEnable() => Application.onBeforeRender += PositionLabel;
    private void OnDisable()
    {
        foreach (var interactor in grabInteractors)
            if (interactor != null) interactor.selectEntered.RemoveListener(OnGrabbed);
        grabInteractors.Clear();
        Application.onBeforeRender -= PositionLabel;
        if (follow != null) follow.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (follow != null) Destroy(follow.gameObject);
        if (leaderMesh != null) Destroy(leaderMesh);
        if (leaderMaterial != null) Destroy(leaderMaterial);
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
        prefix.text = failure ? "" : FormatPrefix(prefixText);
        action.text = failure
            ? "<font=\"Inter-ExtraLight SDF\">Move </font>CLOSER<font=\"Inter-ExtraLight SDF\"> to the Object</font>" : actionText;
        LayoutText();
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
        if (visible && follow.target != camera.transform)
        {
            follow.gameObject.SetActive(false);
            follow.target = camera.transform;
        }
        follow.gameObject.SetActive(visible);
        if (!visible) return;

        float opacity = visual.RevealOpacity * messageOpacity;
        if (completedAt >= 0f)
        {
            float elapsed = Time.unscaledTime - completedAt;
            if (!successText && elapsed >= 0.11f)
            {
                successText = true;
                prefix.text = "";
                action.text = completionText;
                LayoutText();
            }
            opacity *= successText ? Mathf.SmoothStep(0f, 1f, (elapsed - 0.11f) / 0.11f)
                : 1f - Mathf.SmoothStep(0f, 1f, elapsed / 0.11f);
        }
        if (fadeText) opacity *= Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - textAppeared) / 0.25f);
        prefix.alpha = opacity;
        action.alpha = opacity;
        UpdateLeader(camera, opacity);
    }

    private void LayoutText()
    {
        prefix.ForceMeshUpdate(true);
        action.ForceMeshUpdate(true);
        int count = prefix.textInfo.characterCount;
        action.transform.localPosition = count > 0
            ? Vector3.right * prefix.textInfo.characterInfo[count - 1].xAdvance
            : Vector3.zero;
        Bounds ink = action.textBounds;
        ink.center += action.transform.localPosition;
        if (count > 0) ink.Encapsulate(prefix.textBounds);
        float scale = Mathf.Min(0.034f / Mathf.Max(0.01f, ink.size.y),
            0.86f / Mathf.Max(0.01f, ink.size.x));
        label.localScale = Vector3.one * scale;
        label.localPosition = -ink.center * scale;
        textLeft = -ink.extents.x * scale;
    }

    private void CreateLeader(Transform root)
    {
        var shader = Resources.Load<Shader>("TutorialDesign/TutorialLeader");
        if (shader == null) return;

        var line = new GameObject("Tutorial Button Leader", typeof(MeshFilter), typeof(MeshRenderer));
        line.transform.SetParent(root, false);
        leaderMesh = new Mesh { name = "Tutorial Button Leader" };
        leaderMesh.MarkDynamic();
        leaderMesh.vertices = leaderVertices;
        leaderMesh.triangles = new[] { 0, 1, 2, 2, 1, 3, 4, 5, 6, 6, 5, 7, 8, 9, 10, 10, 9, 11, 12, 13, 14, 14, 13, 15 };
        line.GetComponent<MeshFilter>().sharedMesh = leaderMesh;
        leaderRenderer = line.GetComponent<MeshRenderer>();
        leaderRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        leaderRenderer.receiveShadows = false;
        leaderMaterial = new Material(shader);
        // The controller materials use ZTest Always with a custom render queue of
        // 4996. Draw the leader last (5000 is Unity's maximum) so the short line
        // over the shell is in front of the controller rather than covered by it.
        leaderMaterial.renderQueue = 5000;
        leaderRenderer.sharedMaterial = leaderMaterial;
    }

    private void CacheButtonSurface()
    {
        foreach (var skin in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var materials = skin.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
            {
                var material = materials[slot];
                if (material == null || !material.name.StartsWith("Controller Side Grip White")
                    || skin.sharedMesh == null || slot >= skin.sharedMesh.subMeshCount) continue;

                // Read the same submesh the visual lights up. Bake once so this
                // also works with imported meshes that are not CPU-readable.
                // useScale must be true: the default bakes the hierarchy scale
                // (0.01 rig x reveal scale) into the vertices, and TransformPoint
                // below would then apply it a second time, collapsing every
                // point onto the controller origin.
                var baked = new Mesh();
                skin.BakeMesh(baked, true);
                var vertices = baked.vertices;
                var normals = baked.normals;
                int[] indices = baked.GetTriangles(slot);
                int[] allTriangles = baked.triangles;
                Destroy(baked);
                if (indices.Length < 3) continue;

                var skinTransform = skin.transform;
                shellTriangles = allTriangles;
                shellVertices = new Vector3[vertices.Length];
                projectedShell = new Vector2[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                    shellVertices[i] = transform.InverseTransformPoint(skinTransform.TransformPoint(vertices[i]));
                buttonTriangles = new Vector3[indices.Length];
                buttonNormals = new Vector3[indices.Length / 3];
                for (int i = 0; i < indices.Length; i++)
                    buttonTriangles[i] = grip.InverseTransformPoint(skinTransform.TransformPoint(vertices[indices[i]]));
                for (int t = 0; t < buttonNormals.Length; t++)
                {
                    Vector3 a = buttonTriangles[t * 3], b = buttonTriangles[t * 3 + 1], c = buttonTriangles[t * 3 + 2];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    if (normals.Length == vertices.Length)
                    {
                        Vector3 shaded = normals[indices[t * 3]] + normals[indices[t * 3 + 1]] + normals[indices[t * 3 + 2]];
                        shaded = grip.InverseTransformDirection(skinTransform.TransformDirection(shaded));
                        if (shaded.sqrMagnitude > 1e-12f) normal = shaded;
                    }
                    buttonNormals[t] = normal.normalized;
                }
                return;
            }
        }
    }

    /// <summary>
    /// Finds the point on the lit button's outer surface at the centre of the part
    /// the viewer can see. <paramref name="visible"/> is the camera-facing share of
    /// the button's area (0 when it faces away and the Cull Back shader hides it).
    /// </summary>
    private bool FindButtonPoint(Vector3 eye, out Vector3 point, out float visible)
    {
        Vector3 localEye = grip.InverseTransformPoint(eye);
        Vector3 weighted = Vector3.zero;
        float weight = 0f, total = 0f;
        for (int t = 0; t < buttonNormals.Length; t++)
        {
            Vector3 a = buttonTriangles[t * 3], b = buttonTriangles[t * 3 + 1], c = buttonTriangles[t * 3 + 2];
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            Vector3 centroid = (a + b + c) / 3f;
            float facing = Vector3.Dot(buttonNormals[t], (localEye - centroid).normalized);
            total += area;
            if (facing <= 0f) continue;
            weighted += centroid * (area * facing);
            weight += area * facing;
        }
        visible = total > 0f ? weight / total : 0f;
        point = default;
        if (weight <= 0f) return false;

        // The centroid of a curved patch lies just beneath it. Cast the view ray
        // through it and keep the nearest camera-facing hit, which is on the surface.
        Vector3 target = weighted / weight;
        Vector3 ray = (target - localEye).normalized;
        float nearest = float.PositiveInfinity;
        for (int t = 0; t < buttonNormals.Length; t++)
        {
            Vector3 a = buttonTriangles[t * 3];
            if (Vector3.Dot(buttonNormals[t], localEye - a) <= 0f) continue;
            Vector3 edge1 = buttonTriangles[t * 3 + 1] - a, edge2 = buttonTriangles[t * 3 + 2] - a;
            Vector3 p = Vector3.Cross(ray, edge2);
            float det = Vector3.Dot(edge1, p);
            if (Mathf.Abs(det) < 1e-9f) continue;
            Vector3 fromCorner = localEye - a;
            float u = Vector3.Dot(fromCorner, p) / det;
            if (u < 0f || u > 1f) continue;
            Vector3 q = Vector3.Cross(fromCorner, edge1);
            float v = Vector3.Dot(ray, q) / det;
            if (v < 0f || u + v > 1f) continue;
            float distance = Vector3.Dot(edge2, q) / det;
            if (distance > 0f && distance < nearest) nearest = distance;
        }
        point = grip.TransformPoint(float.IsPositiveInfinity(nearest) ? target : localEye + ray * nearest);
        return true;
    }

    private void UpdateLeader(Camera camera, float opacity)
    {
        if (leaderRenderer == null) return;
        Vector3 eye = camera.transform.position;
        Vector3 start = default;
        float visible = 0f;
        bool found = buttonTriangles != null && opacity > 0f && FindButtonPoint(eye, out start, out visible);
        // When the button turns away it is not drawn, so fade the leader out
        // rather than pointing at some other part of the shell.
        float fade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.08f, visible));
        leaderRenderer.enabled = found && fade > 0f
            && camera.WorldToViewportPoint(start).z > camera.nearClipPlane;
        if (!leaderRenderer.enabled) return;

        var root = follow.transform;
        Vector3 bar = new Vector3(textLeft - TextGap, 0f, 0f);
        Vector3 elbow = bar - Vector3.right * StubLength;
        Vector3 elbowWorld = root.TransformPoint(elbow);
        Vector3 startLocal = root.InverseTransformPoint(start);

        // Short line: from the button surface to just outside the controller's
        // outline, along the text's axes: right for the grip, up for Y.
        Vector3 stubDirection = FindStubDirection(eye, start, root, out float exit);
        Vector3 knee = startLocal + stubDirection * Mathf.Max(exit + ButtonStubMargin, ButtonStubMinLength);
        Vector3 kneeWorld = root.TransformPoint(knee);

        Vector3 diagonal = elbowWorld - kneeWorld;
        Vector3 normal = Vector3.Cross(diagonal, elbowWorld - eye).normalized;
        if (normal.sqrMagnitude < 0.5f) normal = root.up;
        // The button is much nearer than the label. Narrow the near end so the
        // line keeps one apparent width and does not cover the button it marks.
        float nearWidth = LineWidth * Mathf.Clamp(
            (start - eye).magnitude / Mathf.Max(0.01f, (elbowWorld - eye).magnitude), 0.2f, 1f);
        Vector3 stubNormal = Mathf.Abs(stubDirection.x) > 0.5f ? Vector3.up : Vector3.right;
        SetLeaderSegment(12, startLocal, knee, stubNormal, nearWidth, nearWidth);
        SetLeaderSegment(0, knee, elbow, root.InverseTransformDirection(normal), nearWidth);
        SetLeaderSegment(4, elbow, bar, Vector3.up);
        SetLeaderSegment(8, bar - Vector3.up * (BarHeight * 0.5f),
            bar + Vector3.up * (BarHeight * 0.5f), Vector3.right);
        leaderMesh.vertices = leaderVertices;
        leaderMesh.RecalculateBounds();
        leaderMaterial.SetColor("_Color", new Color(1f, 1f, 1f, opacity * fade));
    }

    /// <summary>
    /// Returns the short line's direction (label right for the grip, label up for Y)
    /// and how far, along it, the controller's outline extends from the button point.
    /// </summary>
    private Vector3 FindStubDirection(Vector3 eye, Vector3 start, Transform root, out float exit)
    {
        // Project the shell from the eye onto the plane through the button point
        // that is parallel to the label, in label right/up coordinates.
        Vector3 planeNormal = root.forward, right = root.right, up = root.up;
        float planeDepth = Vector3.Dot(start - eye, planeNormal);
        var localToWorld = transform.localToWorldMatrix;
        for (int i = 0; i < shellVertices.Length; i++)
        {
            Vector3 fromEye = localToWorld.MultiplyPoint3x4(shellVertices[i]) - eye;
            float depth = Vector3.Dot(fromEye, planeNormal);
            Vector3 onPlane = eye + fromEye * (planeDepth / Mathf.Max(depth, 0.001f)) - start;
            projectedShell[i] = new Vector2(Vector3.Dot(onPlane, right), Vector3.Dot(onPlane, up));
        }

        // Farthest outline crossing to the right of, and above, the button point.
        float exitRight = 0f, exitUp = 0f;
        for (int i = 0; i < shellTriangles.Length; i += 3)
        {
            for (int edge = 0; edge < 3; edge++)
            {
                Vector2 a = projectedShell[shellTriangles[i + edge]];
                Vector2 b = projectedShell[shellTriangles[i + (edge + 1) % 3]];
                if ((a.y > 0f) != (b.y > 0f))
                    exitRight = Mathf.Max(exitRight, Mathf.Lerp(a.x, b.x, a.y / (a.y - b.y)));
                if ((a.x > 0f) != (b.x > 0f))
                    exitUp = Mathf.Max(exitUp, Mathf.Lerp(a.y, b.y, a.x / (a.x - b.x)));
            }
        }

        // Fixed per button: side buttons (grip, trigger) lead out to the right,
        // face buttons (Y, X, ...) lead straight up.
        bool sideButton = anchorBone.Contains("grip") || anchorBone.Contains("trigger");
        exit = sideButton ? exitRight : exitUp;
        return sideButton ? Vector3.right : Vector3.up;
    }

    private void SetLeaderSegment(int index, Vector3 start, Vector3 end, Vector3 normal,
        float startWidth = LineWidth, float endWidth = LineWidth)
    {
        Vector3 halfStart = normal * (startWidth * 0.5f);
        Vector3 halfWidth = normal * (endWidth * 0.5f);
        leaderVertices[index] = start - halfStart;
        leaderVertices[index + 1] = start + halfStart;
        leaderVertices[index + 2] = end - halfWidth;
        leaderVertices[index + 3] = end + halfWidth;
    }
}
