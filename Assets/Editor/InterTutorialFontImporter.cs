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
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/TutorialDesign/" + name + ".otf");
        if (font == null) throw new System.InvalidOperationException("Missing Inter font: " + name);
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destination);
        if (existing != null)
        {
            const string characters = "PRESS Y To GRAB CRAWLWELL DONE! You've got it!Move CLOSER to the ObjectNow SWING Your Arms to MOVE";
            if (!existing.HasCharacters(characters))
            {
                try
                {
                    existing.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    // TMP's Dynamic setter uses a nonserialized editor cache.
                    // After a domain reload that cache can be null; explicitly
                    // restore the serialized source before loading the face.
                    var serialized = new SerializedObject(existing);
                    serialized.FindProperty("m_SourceFontFile").objectReferenceValue = font;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    existing.isMultiAtlasTexturesEnabled = true;
                    if (!existing.TryAddCharacters(characters, out string absent) && !string.IsNullOrEmpty(absent))
                        throw new System.InvalidOperationException("Missing hint glyphs in " + name + ": " + absent);
                    foreach (var texture in existing.atlasTextures)
                        if (texture != null && !AssetDatabase.Contains(texture))
                            AssetDatabase.AddObjectToAsset(texture, existing);
                }
                finally
                {
                    existing.atlasPopulationMode = AtlasPopulationMode.Static;
                    EditorUtility.SetDirty(existing);
                }
            }
            existing.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(existing);
            return;
        }
        var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA,
            512, 512, AtlasPopulationMode.Dynamic);
        asset.name = name + " SDF";
        if (!asset.TryAddCharacters("PRESS Y To GRAB CRAWLWELL DONE! You've got it!Move Closer to the ObjectNow SWING Your Arms to MOVE", out string missing))
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
