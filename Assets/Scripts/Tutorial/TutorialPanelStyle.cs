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
    [Min(0f)] public float textForwardDepth = 0.5f;
    [Range(0f, 30f)] public float textTiltDegrees = 10f;

    [Header("UI appearance")]
    [Min(0.05f)] public float fadeDuration = 0.35f;
    [Range(0f, 1f)] public float backdropOpacity = 0.5f;

    [Header("Materials")]
    public Shader frostedShader;
    public Shader overlayShader;

    [Header("Controller display")]
    public GameObject controllerDisplayPrefab;
    [Min(0.01f)] public float controllerHeightRatio = 1f;
    [Min(0f)] public float controllerForwardOffset = 0.5f;
    public float controllerHorizontalOffset;
    [Range(0f, 90f)] public float controllerRotationAmplitude = 15f;
    [Min(0.1f)] public float controllerRotationPeriod = 4f;

    [Header("Controller handoff")]
    public GameObject handControllerPrefab;
    [Min(0.1f)] public float controllerSnapDuration = 0.85f;
    [Min(0.01f)] public float controllerSnapFadeDistance = 0.35f;
    [Min(0f)] public float controllerSnapFadeEndDistance = 0.1f;
    [Min(0.01f)] public float controllerHandFadeDuration = 0.65f;
    [Min(0.01f)] public float controllerDisplayFadeDuration = 0.18f;
    [Header("Panel docking motion")]
    [Min(0.1f)] public float panelDockDuration = 1.25f;
    public Vector3 dockedPanelPosition = new Vector3(0f, -0.5f, 0.45f);
    [Range(0f, 90f)] public float dockedPanelPitch = 30f;
    [Range(0.01f, 1f)] public float dockedPanelScale = 0.4f;
    [Range(0f, 1f)] public float dockedPanelOpacityMultiplier = 0.8f;
    [Min(0f)] public float dockedForegroundDepth = 0.02f;

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
