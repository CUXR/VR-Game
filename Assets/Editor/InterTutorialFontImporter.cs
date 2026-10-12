using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class InterTutorialFontImporter
{
    [MenuItem("VR Game/UI/Prepare Inter Prompt Fonts")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Create("Inter-ExtraLight");
        Create("Inter-Medium");
        AssetDatabase.SaveAssets();
    }

    private static void Create(string name)
    {
        string destination = "Assets/Resources/TutorialDesign/" + name + " SDF.asset";
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destination) != null) return;
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/TutorialDesign/" + name + ".otf");
        if (font == null) throw new System.InvalidOperationException("Missing Inter font: " + name);
        var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA,
            512, 512, AtlasPopulationMode.Dynamic);
        asset.name = name + " SDF";
        if (!asset.TryAddCharacters("PRESS To GRAB", out string missing))
            throw new System.InvalidOperationException("Missing prompt glyphs: " + missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        asset.material.shader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Assets/Resources/TutorialDesign/TutorialTextOverlay.shader");
        asset.material.SetFloat("_UIFade", 1f);
        asset.material.renderQueue = 5000;
        AssetDatabase.CreateAsset(asset, destination);
        foreach (var texture in asset.atlasTextures)
            if (texture != null && !AssetDatabase.Contains(texture))
                AssetDatabase.AddObjectToAsset(texture, asset);
        if (!AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset);
    }
}
