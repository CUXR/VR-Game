using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class MonomaniacFontImporter
{
    private const string FontPath = "Assets/Resources/TutorialDesign/MonomaniacOne-Regular.ttf";
    private const string AssetPath = "Assets/Resources/TutorialDesign/MonomaniacOne SDF.asset";

    [MenuItem("Tools/Tutorial/Generate Monomaniac One Font Asset")]
    public static void Generate()
    {
        AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceUpdate);
        Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (source == null)
            throw new System.InvalidOperationException("Monomaniac One source font was not imported.");

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
        {
            Debug.Log("Monomaniac One TMP font asset is already available.");
            return;
        }

        FontEngine.InitializeFontEngine();
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source, 90, 9,
            GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        if (asset == null)
            throw new System.InvalidOperationException("Could not generate the Monomaniac One TMP font asset.");

        asset.name = "MonomaniacOne SDF";
        AssetDatabase.CreateAsset(asset, AssetPath);
        foreach (Texture2D texture in asset.atlasTextures)
        {
            if (texture != null && !AssetDatabase.Contains(texture))
                AssetDatabase.AddObjectToAsset(texture, asset);
        }
        if (asset.material != null && !AssetDatabase.Contains(asset.material))
            AssetDatabase.AddObjectToAsset(asset.material, asset);

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("Monomaniac One TMP font asset created at " + AssetPath);
    }
}
