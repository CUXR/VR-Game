using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "Curved Tutorial Panel Style", menuName = "VR Game/UI/Curved Tutorial Panel Style")]
public sealed class TutorialPanelStyle : ScriptableObject
{
    [Header("Panel geometry (metres and Figma pixels)")]
    [Min(0.1f)] public float headsetDistance = 1.5f;
    public float verticalOffset = -0.18f;
    [Min(0.00001f)] public float canvasScale = 0.00065f;
    [Min(1f)] public float panelWidth = 2100f;
    [Min(1f)] public float panelHeight = 900f;
    [Min(0f)] public float cornerRadius = 140f;
    [Min(0f)] public float foregroundDepth = 0.2f;
    [Min(1)] public int curveColumns = 64;
    [Min(1)] public int curveRows = 16;

    [Header("Text")]
    public TMP_FontAsset font;
    public Shader textShader;
    [Min(1f)] public float textFontSize = 43.666f;
    [Min(1f)] public float grabReferenceInkWidth = 381f;

    [Header("Materials")]
    public Shader frostedShader;
    public Shader overlayShader;

    [Header("Terminal animation")]
    [Min(0.001f)] public float typewriterSecondsPerCharacter = 0.055f;
    [Min(0.001f)] public float terminalScrollDuration = 0.36f;
    [Min(0f)] public float terminalLinePause = 0.24f;
    [Min(0f)] public float terminalInitialDelay = 0.2f;
    public float terminalBottomRowY = 1135f;
    [Min(0f)] public float terminalRowSpacing = 61f;
    [Min(0.001f)] public float indicatorPulseHz = 1.2f;
    [Min(0.001f)] public float cursorBlinkHz = 1.6f;
    [Range(0f, 1f)] public float cursorBlinkDuty = 0.55f;
}
