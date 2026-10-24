using UnityEditor;
using UnityEngine;

public static class TutorialHintAssetBuilder
{
    [MenuItem("VR Game/UI/Prepare Reusable Controller Hints")]
    public static void Build()
    {
        const string stylePath = "Assets/Resources/TutorialDesign/Attention Style.asset";
        if (AssetDatabase.LoadAssetAtPath<TutorialAttentionStyle>(stylePath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TutorialAttentionStyle>(), stylePath);
        TutorialTrackedControllerImporter.BuildCrawl();
        InterTutorialFontImporter.Build();
        Create("Grab Hint", "Tracked Left Touch Plus", "left_b_trigger_grip", "PRESS To ", "GRAB", "Wire", "", true);
        Create("Crawl Hint", "Tracked Left Y Touch Plus", "b_button_y", "PRESS Y To ", "CRAWL", "", "Incinerator Vent", false);
        AssetDatabase.SaveAssets();
    }
    private static void Create(string name, string prefab, string bone, string prefix, string action,
        string tag, string targetName, bool attention)
    {
        string path = "Assets/Resources/TutorialDesign/" + name + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<ControllerHintDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<ControllerHintDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }
        definition.controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TutorialDesign/Prefabs/" + prefab + ".prefab");
        definition.anchorBone = bone; definition.prefix = prefix; definition.action = action;
        definition.targetTag = tag; definition.targetName = targetName; definition.animateTarget = attention;
        definition.attentionStyle = AssetDatabase.LoadAssetAtPath<TutorialAttentionStyle>("Assets/Resources/TutorialDesign/Attention Style.asset");
        if (action == "CRAWL")
        {
            definition.normalTextOffset = new Vector2(-0.23f, 0f);
            definition.rightTextOffset = 0.60f;
        }
        EditorUtility.SetDirty(definition);
    }
}
