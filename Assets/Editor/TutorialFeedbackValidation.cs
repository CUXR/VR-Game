using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Run once after this revision compiles in the existing editor. Never enter
// Play Mode or change the user's open scene to validate tutorial resources.
[InitializeOnLoad]
public static class TutorialFeedbackValidation
{
    private const string Key = "TutorialFeedbackValidation.v9";
    static TutorialFeedbackValidation() => EditorApplication.update += Check;
    private static void Check()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Check;
        if (SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, true);
        Validate();
    }

    [MenuItem("VR Game/UI/Validate Tutorial Feedback")]
    public static void Validate()
    {
        Directory.CreateDirectory("Logs");
        try
        {
            InterTutorialFontImporter.Build();
            foreach (string name in new[] { "Inter-ExtraLight", "Inter-Medium" })
            {
                var font = Resources.Load<TMP_FontAsset>("TutorialDesign/" + name + " SDF");
                if (font == null || !font.HasCharacters("WELL DONE! You've got it!Move Closer to the ObjectNow SWING Your Arms to MOVE")) throw new Exception(name + " is missing feedback glyphs");
            }
            foreach (string path in new[] {
                "Assets/TutorialDesign/Shaders/ControllerDisplay.shader",
                "Assets/Resources/TutorialDesign/WireSphereGlow.shader" })
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Shader validation failed: " + path);
            }
            foreach (string quality in new[] { "Performant", "Balanced", "HighFidelity" })
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/URP-" + quality + ".asset");
                if (pipeline == null || !pipeline.useRenderingLayers) throw new Exception("Wire light isolation is unavailable in " + quality);
            }
            File.WriteAllText("Logs/tutorial-feedback-validation.txt", "PASS: scripts compiled; feedback glyphs prepared; tutorial shaders report no import errors; isolated lighting supported in every quality tier.\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("Tutorial feedback resource validation passed.");
        }
        catch (Exception error)
        {
            File.WriteAllText("Logs/tutorial-feedback-validation.txt", error.ToString());
            Debug.LogException(error);
        }
    }
}
