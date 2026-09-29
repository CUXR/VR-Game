using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialController : MonoBehaviour
{
    private float HeadsetDistance => style.headsetDistance;
    private float VerticalOffset => style.verticalOffset;
    private float CanvasScale => style.canvasScale;
    private float NewDesignWidth => style.panelWidth;
    private float NewDesignHeight => style.panelHeight;
    private int CurveColumns => style.curveColumns;
    private int CurveRows => style.curveRows;
    private const float FigmaPanelX = 10407f;
    private const float FigmaPanelY = 2618f;
    private const float FigmaFrameX = 9604f;
    private const float FigmaFrameY = 2235f;
    private float NewForegroundDepth => style.foregroundDepth;
    // Visible ink width of the original Figma Grab PNG, measured in pixels.
    private float GrabReferenceInkWidth => style.grabReferenceInkWidth;
    private float TerminalBottomRowY => style.terminalBottomRowY;
    private float TerminalRowSpacing => style.terminalRowSpacing;
    private static readonly string[] TerminalLines =
    {
        ">_ GRAB MODULE — TEST ",
        ">_ WAITING FOR TESTING",
        ">_ ACTION DETECTED",
        "[████████████] 100%",
        ">_ SUCCESS!"
    };
    private static readonly float[] TerminalLineX =
        { 1987f, 1987f, 1987f, 2034f, 1987f };

    public static TutorialController Instance { get; private set; }
    // Retains the original team's prefab field; its legacy text is hidden at runtime.
    public TextMeshProUGUI uiTextElement;
    [SerializeField] private TutorialPanelStyle panelStyle;
    private TutorialPanelStyle style;
    private bool ownsStyle;
    public bool useRightGripToToggle = true;

    private RectTransform promptRoot;
    private Camera headsetCamera;
    private Material frostedMaterial;
    private InputAction rightGripAction;
    private bool targetVisible;
    private Mesh curvedMesh;
    private Mesh[] foregroundMeshes;
    private Material[] foregroundMaterials;
    private readonly List<TextMeshPro> figmaLabels = new List<TextMeshPro>();
    private readonly List<Material> figmaTextMaterials = new List<Material>();
    private readonly Dictionary<TextMeshPro, Vector2> figmaLabelOrigins =
        new Dictionary<TextMeshPro, Vector2>();
    private readonly Dictionary<TextMeshPro, float> figmaLabelDepths =
        new Dictionary<TextMeshPro, float>();
    private readonly Dictionary<TextMeshPro, Vector3[][]> figmaFlatVertices =
        new Dictionary<TextMeshPro, Vector3[][]>();
    private readonly TextMeshPro[] terminalLabels = new TextMeshPro[5];
    private readonly TextMeshPro[] terminalShadows = new TextMeshPro[5];
    private TerminalTypewriterSequence terminalAnimation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureController()
    {
        if (Instance == null)
            new GameObject("Tutorial Controller").AddComponent<TutorialController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (uiTextElement != null)
            uiTextElement.gameObject.SetActive(false);
        style = panelStyle != null ? panelStyle : Resources.Load<TutorialPanelStyle>(
            "TutorialDesign/Curved Tutorial Panel Style");
        if (style == null)
        {
            style = ScriptableObject.CreateInstance<TutorialPanelStyle>();
            ownsStyle = true;
            Debug.LogWarning("Tutorial panel style is missing; using built-in defaults.", this);
        }
        terminalAnimation = new TerminalTypewriterSequence(TerminalLines,
            style.typewriterSecondsPerCharacter, style.terminalScrollDuration,
            style.terminalLinePause, style.terminalInitialDelay);
        BuildCurvedPrompt();
        if (useRightGripToToggle)
        {
            rightGripAction = new InputAction("Show Tutorial", InputActionType.Button);
            rightGripAction.AddBinding("<XRController>{RightHand}/gripPressed");
            rightGripAction.AddBinding("<XRController>{RightHand}/{GripButton}");
            rightGripAction.Enable();
            promptRoot.gameObject.SetActive(false);
        }
        else
        {
            targetVisible = true;
        }
    }

    private void Update()
    {
        if (useRightGripToToggle && rightGripAction != null)
        {
            if (rightGripAction.WasPressedThisFrame())
            {
                targetVisible = !targetVisible;
                if (targetVisible)
                    ResetTerminal();
                promptRoot.gameObject.SetActive(targetVisible);
            }
        }

        if (targetVisible || !useRightGripToToggle)
            UpdateTerminal();

        if (foregroundMaterials != null && foregroundMaterials[1] != null)
        {
            // The grip ring and terminal cursor pulse while the prompt is visible.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI * style.indicatorPulseHz);
            foregroundMaterials[1].SetFloat("_Opacity",
                Mathf.Lerp(0.12f, 1f, pulse));
            float cursorOpacity = terminalAnimation.IsTyping
                || Mathf.Repeat(Time.unscaledTime * style.cursorBlinkHz, 1f)
                    < style.cursorBlinkDuty ? 1f : 0f;
            if (foregroundMaterials[5] != null)
                foregroundMaterials[5].SetFloat("_Opacity",
                    cursorOpacity);
            if (foregroundMaterials[6] != null)
                foregroundMaterials[6].SetFloat("_Opacity",
                    cursorOpacity);
        }
    }

    private void LateUpdate()
    {
        if (headsetCamera == null || !headsetCamera.isActiveAndEnabled)
            headsetCamera = Camera.main;
        if (headsetCamera == null)
            return;

        if (promptRoot.parent != headsetCamera.transform)
        {
            promptRoot.SetParent(headsetCamera.transform, false);
        }

        // Keep the completed panel 1.5 m from the headset.
        float forward = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        promptRoot.localPosition = new Vector3(0f, VerticalOffset, forward);
        promptRoot.localRotation = Quaternion.identity;
        promptRoot.localScale = Vector3.one * CanvasScale;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        rightGripAction?.Dispose();
        if (ownsStyle && style != null)
            Destroy(style);
        if (frostedMaterial != null)
            Destroy(frostedMaterial);
        if (curvedMesh != null)
            Destroy(curvedMesh);
        if (foregroundMeshes != null)
            foreach (Mesh mesh in foregroundMeshes)
                if (mesh != null) Destroy(mesh);
        if (foregroundMaterials != null)
            foreach (Material material in foregroundMaterials)
                if (material != null) Destroy(material);
        foreach (Material material in figmaTextMaterials)
            if (material != null) Destroy(material);
        if (promptRoot != null)
            Destroy(promptRoot.gameObject);
    }

    // Compatibility with the original ObjectInteraction script.
    public void DisplayText(InteractableInterface interactableObject) { }
    public void ClearText() { }

    private void PreparePanel()
    {
        frostedMaterial.SetFloat("_PanelWidth", NewDesignWidth);
        frostedMaterial.SetFloat("_PanelHeight", NewDesignHeight);
        frostedMaterial.SetFloat("_CornerRadius", style.cornerRadius);
        BuildCurvedMesh(NewDesignWidth);
        if (foregroundMaterials != null)
        {
            for (int i = 0; i < foregroundMaterials.Length; i++)
                if (foregroundMaterials[i] != null && i != 1)
                    foregroundMaterials[i].SetFloat("_Opacity", OverlayAlpha(i));
        }
    }

    private void BuildCurvedPrompt()
    {
        GameObject root = new GameObject("Figma 53:2 Curved Frosted Panel",
            typeof(RectTransform), typeof(MeshFilter), typeof(MeshRenderer));
        promptRoot = root.GetComponent<RectTransform>();
        promptRoot.sizeDelta = new Vector2(NewDesignWidth, NewDesignHeight);
        curvedMesh = new Mesh { name = "Curved tutorial glass" };
        root.GetComponent<MeshFilter>().sharedMesh = curvedMesh;

        Shader shader = style.frostedShader != null ? style.frostedShader
            : Resources.Load<Shader>("TutorialDesign/TutorialFrostedGlass");
        if (shader == null)
        {
            Debug.LogError("Tutorial frosted glass shader is missing.", this);
            root.SetActive(false);
            return;
        }
        frostedMaterial = new Material(shader);
        // Render after scene transparents; depth testing is disabled in the shader.
        frostedMaterial.renderQueue = 4990;
        root.GetComponent<MeshRenderer>().sharedMaterial = frostedMaterial;
        BuildFigmaOverlays();
        PreparePanel();
    }

    private void BuildFigmaOverlays()
    {
        Shader overlayShader = style.overlayShader != null ? style.overlayShader
            : Resources.Load<Shader>("TutorialDesign/TutorialFigmaOverlay");
        if (overlayShader == null)
        {
            Debug.LogError("Tutorial Figma overlay shader is missing.", this);
            return;
        }

        foregroundMeshes = new Mesh[7];
        foregroundMaterials = new Material[7];
        // Absolute Figma bounds are converted from the 2100 x 900 glass node.
        CreateFigmaOverlay(2, "Left Controller Shadow (54:82)",
            "figma-left-controller-shadow", overlayShader,
            1005f + FigmaFrameX, 601f + FigmaFrameY,
            334.0408f, 478.5183f, 12, 8, forwardDepth: 0f);
        CreateFigmaOverlay(3, "Flash Indicator Shadow (54:85)",
            "figma-flash-indicator-shadow", overlayShader,
            1257f + FigmaFrameX, 913f + FigmaFrameY,
            42.5836f, 42.5836f, 4, 4, forwardDepth: 0f);
        CreateFigmaOverlay(0, "Left Controller (53:64)", "figma-left-controller",
            overlayShader, 10606f, 2833f, 326.0408f, 470.5183f, 12, 8);
        CreateFigmaOverlay(1, "Flashing Indicator (53:58)", "figma-flash-indicator",
            overlayShader, 10858f, 3145f, 34.5836f, 34.5836f, 4, 4);
        CreateFigmaOverlay(4, "Divider (54:71)", null, overlayShader,
            1853f + FigmaFrameX, 506f + FigmaFrameY, 1f, 654f, 1, 12, 0.6f);
        CreateFigmaOverlay(5, "Terminal Indicator Shadow (54:99)",
            "figma-terminal-indicator-shadow", overlayShader,
            2427f + FigmaFrameX, 971f + FigmaFrameY,
            28f, 42f, 1, 1, forwardDepth: 0f);
        CreateFigmaOverlay(6, "Terminal Indicator (54:72)", null,
            overlayShader, 2426f + FigmaFrameX, 969f + FigmaFrameY,
            20f, 34f, 1, 1);

        BuildFigmaText();
    }

    private static float OverlayAlpha(int index)
    {
        return index == 4 ? 0.6f : 1f;
    }

    private void CreateFigmaOverlay(int index, string name, string textureName,
        Shader shader, float figmaX, float figmaY, float width, float height,
        int columns, int rows, float alpha = 1f, float blurPixels = 0f,
        float forwardDepth = -1f)
    {
        if (forwardDepth < 0f) forwardDepth = NewForegroundDepth;
        Texture2D texture = textureName == null ? Texture2D.whiteTexture
            : Resources.Load<Texture2D>("TutorialDesign/" + textureName);
        if (texture == null)
        {
            Debug.LogError("Missing Figma texture: " + textureName, this);
            return;
        }

        GameObject layer = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        layer.transform.SetParent(promptRoot, false);
        Mesh mesh = new Mesh { name = name + " curved mesh" };
        foregroundMeshes[index] = mesh;
        int verticesPerRow = columns + 1;
        Vector3[] vertices = new Vector3[verticesPerRow * (rows + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[columns * rows * 6];
        float radius = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        float left = figmaX - FigmaPanelX - NewDesignWidth * 0.5f;
        float bottom = NewDesignHeight * 0.5f - (figmaY - FigmaPanelY) - height;
        for (int y = 0; y <= rows; y++)
        {
            float v = y / (float)rows;
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns;
                float angle = (left + u * width) * CanvasScale / radius;
                int vertex = y * verticesPerRow + x;
                // Move toward the headset along this panel point's sightline.
                // Moving only local Z made left-side art appear farther left.
                Vector3 panelPoint = new Vector3(radius * Mathf.Sin(angle),
                    VerticalOffset + (bottom + v * height) * CanvasScale,
                    radius * Mathf.Cos(angle));
                Vector3 foregroundPoint = panelPoint
                    - panelPoint.normalized * forwardDepth;
                vertices[vertex] = (foregroundPoint
                    - new Vector3(0f, VerticalOffset, radius)) / CanvasScale;
                uv[vertex] = new Vector2(u, v);
            }
        }
        int triangle = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int a = y * verticesPerRow + x;
                int b = a + verticesPerRow;
                int c = a + 1;
                int d = b + 1;
                triangles[triangle++] = a;
                triangles[triangle++] = b;
                triangles[triangle++] = c;
                triangles[triangle++] = b;
                triangles[triangle++] = d;
                triangles[triangle++] = c;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        layer.GetComponent<MeshFilter>().sharedMesh = mesh;
        Material material = new Material(shader);
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Opacity", alpha);
        material.SetFloat("_BlurPixels", blurPixels);
        material.SetFloat("_Solid", textureName == null ? 1f : 0f);
        material.SetFloat("_UseTextureAlpha",
            index == 2 || index == 3 || index == 5 ? 1f : 0f);
        material.renderQueue = index == 2 || index == 3 || index == 5
            ? 4991 : 4992;
        foregroundMaterials[index] = material;
        layer.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void BuildFigmaText()
    {
        TMP_FontAsset font = style.font != null ? style.font
            : Resources.Load<TMP_FontAsset>("TutorialDesign/MonomaniacOne SDF");
        Shader textShader = style.textShader != null ? style.textShader
            : Resources.Load<Shader>("TutorialDesign/TutorialTextOverlay");
        if (font == null || textShader == null)
        {
            Debug.LogError("Tutorial Monomaniac One font or overlay shader is missing.", this);
            return;
        }

        // Every Figma text layer, including its soft shadow, remains editable TMP text.
        AddFigmaLabel("CLICK HERE TO GRAB_ Shadow (54:81)", "CLICK HERE TO GRAB_",
            1315f, 898f, 385f, font, textShader, true);
        terminalShadows[3] = AddFigmaLabel("Battery Bar Shadow (54:88)", "[████████████] 100%",
            2043f, 1080f, 660f, font, textShader, true);
        terminalShadows[0] = AddFigmaLabel("Grab Test Shadow (54:89)", ">_ GRAB MODULE — TEST ",
            1996f, 897f, 429f, font, textShader, true);
        terminalShadows[1] = AddFigmaLabel("Waiting Shadow (54:90)", ">_ WAITING FOR TESTING",
            1996f, 958f, 428f, font, textShader, true);
        terminalShadows[2] = AddFigmaLabel("Action Shadow (54:91)", ">_ ACTION DETECTED",
            1996f, 1019f, 369f, font, textShader, true);
        terminalShadows[4] = AddFigmaLabel("Success Shadow (54:92)", ">_ SUCCESS!",
            1996f, 1141f, 221f, font, textShader, true);
        AddFigmaLabel("Date Shadow (54:93)", "01/01/2076",
            1996f, 480f, 228f, font, textShader, true);
        AddFigmaLabel("Time Shadow (54:94)", "12:08:05",
            2282f, 480f, 160f, font, textShader, true);
        AddFigmaLabel("Battery Shadow (54:95)", "BATTERY 100%",
            2500f, 480f, 277f, font, textShader, true);

        TextMeshPro grabLabel = AddFigmaLabel("CLICK HERE TO GRAB_ (53:65)",
            "CLICK HERE TO GRAB_",
            1308f, 891f, 385f, font, textShader);
        terminalLabels[3] = AddFigmaLabel("Battery Bar (54:75)", "[████████████] 100%",
            2034f, 1074f, 660f, font, textShader);
        terminalLabels[0] = AddFigmaLabel("Grab Test (53:7)", ">_ GRAB MODULE — TEST ",
            1987f, 891f, 429f, font, textShader);
        terminalLabels[1] = AddFigmaLabel("Waiting (53:67)", ">_ WAITING FOR TESTING",
            1987f, 952f, 428f, font, textShader);
        terminalLabels[2] = AddFigmaLabel("Action (54:73)", ">_ ACTION DETECTED",
            1987f, 1013f, 369f, font, textShader);
        terminalLabels[4] = AddFigmaLabel("Success (54:77)", ">_ SUCCESS!",
            1987f, 1135f, 221f, font, textShader);
        AddFigmaLabel("Date (53:36)", "01/01/2076",
            1987f, 474f, 228f, font, textShader);
        AddFigmaLabel("Time (53:38)", "12:08:05",
            2273f, 474f, 160f, font, textShader);
        AddFigmaLabel("Battery (54:79)", "BATTERY 100%",
            2491f, 474f, 277f, font, textShader);

        MatchFigmaTextToGrabReference(grabLabel);
        CurveFigmaLabels();
        ResetTerminal();
    }

    private void ResetTerminal()
    {
        if (terminalLabels[0] == null) return;
        terminalAnimation.Restart(Time.unscaledTime);
        for (int i = 0; i < TerminalLines.Length; i++)
        {
            SetFigmaText(terminalLabels[i], "");
            SetFigmaText(terminalShadows[i], "");
            MoveFigmaLabel(terminalLabels[i], TerminalBottomRowY);
            MoveFigmaLabel(terminalShadows[i], TerminalBottomRowY + 6f);
        }
        UpdateTerminalCursor();
    }

    private void UpdateTerminal()
    {
        if (terminalLabels[0] == null) return;
        terminalAnimation.Tick(Time.unscaledTime);
        int lineIndex = terminalAnimation.LineIndex;
        for (int i = 0; i < TerminalLines.Length; i++)
        {
            int characters = i < lineIndex ? TerminalLines[i].Length
                : i == lineIndex ? terminalAnimation.CharacterCount : 0;
            string typed = TerminalLines[i].Substring(0, characters);
            SetFigmaText(terminalLabels[i], typed);
            SetFigmaText(terminalShadows[i], typed);
            if (i <= lineIndex)
            {
                float rowY = TerminalBottomRowY
                    - TerminalRowSpacing * (lineIndex - i);
                rowY -= TerminalRowSpacing * terminalAnimation.ScrollProgress;
                MoveFigmaLabel(terminalLabels[i], rowY);
                MoveFigmaLabel(terminalShadows[i], rowY + 6f);
            }
        }
        UpdateTerminalCursor();
    }

    private void SetFigmaText(TextMeshPro label, string content)
    {
        if (label == null || label.text == content) return;
        figmaFlatVertices[label] = null;
        label.text = content;
        label.ForceMeshUpdate(true, true);
    }

    private void MoveFigmaLabel(TextMeshPro label, float frameY)
    {
        if (label == null) return;
        Vector2 origin = figmaLabelOrigins[label];
        float top = NewDesignHeight * 0.5f
            - (FigmaFrameY + frameY - FigmaPanelY);
        if (Mathf.Abs(origin.y - top) < 0.001f) return;
        origin.y = top;
        figmaLabelOrigins[label] = origin;
        float radius = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        float angle = origin.x * CanvasScale / radius;
        Vector3 panelPoint = new Vector3(radius * Mathf.Sin(angle),
            VerticalOffset + top * CanvasScale, radius * Mathf.Cos(angle));
        Vector3 foregroundPoint = panelPoint
            - panelPoint.normalized * figmaLabelDepths[label];
        label.transform.localPosition = (foregroundPoint
            - new Vector3(0f, VerticalOffset, radius)) / CanvasScale;
        label.ForceMeshUpdate(true, true);
    }

    private void UpdateTerminalCursor()
    {
        if (foregroundMeshes == null || foregroundMeshes.Length < 7
            || foregroundMeshes[5] == null || foregroundMeshes[6] == null)
            return;
        int lineIndex = terminalAnimation.LineIndex;
        float cursorX = TerminalLineX[lineIndex];
        if (terminalAnimation.CharacterCount > 0)
        {
            TMP_TextInfo info = terminalLabels[lineIndex].textInfo;
            if (info.characterCount > 0)
                cursorX += info.characterInfo[info.characterCount - 1].xAdvance
                    * terminalLabels[lineIndex].transform.localScale.x + 11f;
        }
        float cursorY = TerminalBottomRowY
            - TerminalRowSpacing * terminalAnimation.ScrollProgress + 17f;
        MoveCursorMesh(foregroundMeshes[5], cursorX + 1f, cursorY + 2f,
            28f, 42f, 0f);
        MoveCursorMesh(foregroundMeshes[6], cursorX, cursorY,
            20f, 34f, NewForegroundDepth);
    }

    private void MoveCursorMesh(Mesh mesh, float frameX, float frameY,
        float width, float height, float depth)
    {
        float radius = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        float left = FigmaFrameX + frameX - FigmaPanelX
            - NewDesignWidth * 0.5f;
        float bottom = NewDesignHeight * 0.5f
            - (FigmaFrameY + frameY - FigmaPanelY) - height;
        Vector3[] vertices = mesh.vertices;
        for (int y = 0; y <= 1; y++)
        {
            for (int x = 0; x <= 1; x++)
            {
                float angle = (left + x * width) * CanvasScale / radius;
                Vector3 panelPoint = new Vector3(radius * Mathf.Sin(angle),
                    VerticalOffset + (bottom + y * height) * CanvasScale,
                    radius * Mathf.Cos(angle));
                Vector3 foregroundPoint = panelPoint
                    - panelPoint.normalized * depth;
                vertices[y * 2 + x] = (foregroundPoint
                    - new Vector3(0f, VerticalOffset, radius)) / CanvasScale;
            }
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    private void CurveFigmaLabels()
    {
        foreach (TextMeshPro label in figmaLabels)
        {
            if (label == null) continue;
            label.OnPreRenderText += info => WarpFigmaText(label, info);
            label.ForceMeshUpdate(true, true);
        }
    }

    private void WarpFigmaText(TextMeshPro label, TMP_TextInfo textInfo)
    {
        if (!figmaFlatVertices.TryGetValue(label, out Vector3[][] flatVertices)
            || flatVertices == null
            || flatVertices.Length != textInfo.meshInfo.Length)
        {
            flatVertices = new Vector3[textInfo.meshInfo.Length][];
            for (int i = 0; i < flatVertices.Length; i++)
                flatVertices[i] = (Vector3[])textInfo.meshInfo[i].vertices.Clone();
            figmaFlatVertices[label] = flatVertices;
        }

        Vector2 origin = figmaLabelOrigins[label];
        float forwardDepth = figmaLabelDepths[label];
        float radius = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        float scale = label.transform.localScale.x;
        Quaternion inverseRotation = Quaternion.Inverse(label.transform.localRotation);
        Vector3 rootCenter = new Vector3(0f, VerticalOffset, radius);
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible) continue;
            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;
            if (materialIndex >= flatVertices.Length) continue;
            Vector3[] source = flatVertices[materialIndex];
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            if (vertexIndex + 3 >= source.Length || vertexIndex + 3 >= vertices.Length)
                continue;

            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 flat = source[vertexIndex + corner];
                float angle = (origin.x + flat.x * scale) * CanvasScale / radius;
                Vector3 panelPoint = new Vector3(radius * Mathf.Sin(angle),
                    VerticalOffset + (origin.y + flat.y * scale) * CanvasScale,
                    radius * Mathf.Cos(angle));
                Vector3 foregroundPoint = panelPoint
                    - panelPoint.normalized * forwardDepth;
                Vector3 rootPoint = (foregroundPoint - rootCenter) / CanvasScale;
                vertices[vertexIndex + corner] = inverseRotation
                    * (rootPoint - label.transform.localPosition) / scale;
            }
        }
    }

    private void MatchFigmaTextToGrabReference(TextMeshPro reference)
    {
        // Compare the Unity glyph mesh against the old Grab image, then apply
        // one shared, undistorted scale to all editable text and shadows.
        reference.ForceMeshUpdate(true, true);
        Vector3 rendered = reference.textBounds.size;
        if (rendered.x <= 0f || rendered.y <= 0f)
        {
            Debug.LogWarning("Could not measure the tutorial Grab text glyphs.", this);
            return;
        }

        // Match the original Grab image width with one uniform scale. TMP's
        // text bounds include glyph padding, so using its height as a second
        // independent scale visibly flattened the letters.
        float uniformScale = GrabReferenceInkWidth / rendered.x;
        foreach (TextMeshPro label in figmaLabels)
        {
            if (label == null) continue;
            label.transform.localScale = Vector3.one * uniformScale;
        }
        Debug.Log($"Tutorial text calibrated to Figma Grab image: "
            + $"Unity width {rendered.x:F1}, "
            + $"reference width {GrabReferenceInkWidth:F0}, "
            + $"uniform scale {uniformScale:F3}.", this);
    }

    private TextMeshPro AddFigmaLabel(string name, string content, float frameX,
        float frameY, float width, TMP_FontAsset font, Shader shader,
        bool shadow = false)
    {
        GameObject layer = new GameObject(name, typeof(RectTransform),
            typeof(TextMeshPro));
        layer.transform.SetParent(promptRoot, false);
        layer.transform.localScale = Vector3.one;
        TextMeshPro label = layer.GetComponent<TextMeshPro>();
        label.font = font;
        label.fontSize = style.textFontSize;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.rectTransform.pivot = new Vector2(0f, 1f);
        label.rectTransform.sizeDelta = new Vector2(width, 63f);
        label.margin = Vector4.zero;
        label.text = content;
        label.color = Color.white;
        label.alpha = 1f;

        float figmaX = FigmaFrameX + frameX;
        float figmaY = FigmaFrameY + frameY;
        float left = figmaX - FigmaPanelX - NewDesignWidth * 0.5f;
        float top = NewDesignHeight * 0.5f - (figmaY - FigmaPanelY);
        figmaLabelOrigins.Add(label, new Vector2(left, top));
        float forwardDepth = shadow ? 0f : NewForegroundDepth;
        figmaLabelDepths.Add(label, forwardDepth);
        float radius = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        float angle = left * CanvasScale / radius;
        Vector3 panelPoint = new Vector3(radius * Mathf.Sin(angle),
            VerticalOffset + top * CanvasScale, radius * Mathf.Cos(angle));
        Vector3 foregroundPoint = panelPoint
            - panelPoint.normalized * forwardDepth;
        layer.transform.localPosition = (foregroundPoint
            - new Vector3(0f, VerticalOffset, radius)) / CanvasScale;
        layer.transform.localRotation = Quaternion.Euler(0f,
            angle * Mathf.Rad2Deg, 0f);

        Material material = new Material(font.material);
        material.shader = shader;
        material.SetColor("_FaceColor", new Color(0f, 0f, 0f,
            shadow ? 0.1f : 1f));
        material.SetFloat("_OutlineSoftness", shadow ? 0.15f : 0f);
        material.renderQueue = shadow ? 4991 : 4994;
        label.fontSharedMaterial = material;
        figmaLabels.Add(label);
        figmaTextMaterials.Add(material);
        return label;
    }

    private void BuildCurvedMesh(float width)
    {
        float radius = Mathf.Sqrt(HeadsetDistance * HeadsetDistance
            - VerticalOffset * VerticalOffset);
        CurvedPanelMeshBuilder.Rebuild(curvedMesh, width, NewDesignHeight,
            radius, CanvasScale, CurveColumns, CurveRows);
    }

}
