using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class ClimbingTutorialBuilder
{
    static ClimbingTutorialBuilder() { EditorApplication.update += CheckRequest; }
    static void CheckRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("Temp/climbing-place.request"))
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path == "Assets/Scenes/Demo Level.unity")
            {
                try
                {
                    PlaceInDemoLevel();
                    File.Delete("Temp/climbing-place.request");
                }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
        if (!File.Exists("Temp/climbing-inspect.request")) return;
        File.Delete("Temp/climbing-inspect.request");
        try { BuildAndInspect(); }
        catch(Exception e) { File.WriteAllText("Logs/climbing-validation.txt",e.ToString()); Debug.LogException(e); }
    }
    const string Root="Assets/Resources/TutorialDesign/Climbing/";
    [MenuItem("VR Game/Tutorial/Build Climbing Flow Prefab")]
    public static void Build()
    {
        AssetDatabase.Refresh();
        var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"ClimbStroke.png");
        importer.textureType=TextureImporterType.Default;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.mipmapEnabled=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;
        importer.SaveAndReimport();
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"FlowingClimbStroke.shader");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Climb shader failed import.");
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"FlowingClimbStroke.mat");
        if (!mat) { mat=new Material(shader); AssetDatabase.CreateAsset(mat,Root+"FlowingClimbStroke.mat"); }
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"ClimbStroke.png"));
        mat.SetFloat("_StrokeProgress",0.25f); // Visible layout in Scene view; runtime owns the lifecycle.
        var root=new GameObject("Climbing Wall Flow");
        try
        {
            var flow=root.AddComponent<AlternatingClimbFlow>();
            flow.strokes=new Renderer[6];
            for (int i=0;i<6;i++)
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name=(i%2==0 ? "Left " : "Right ")+(i/2+1);
                quad.transform.SetParent(root.transform,false);
                quad.transform.localPosition=new Vector3(i%2==0 ? -0.22f : 0.22f,i*0.36f,0);
                quad.transform.localScale=new Vector3(0.38f,0.38f*1192.22f/725.372f,1);
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                var renderer=quad.GetComponent<MeshRenderer>();
                renderer.sharedMaterial=mat;
                renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;
                flow.strokes[i]=renderer;
            }
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Climbing Wall Flow.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("VR Game/Tutorial/Place Climbing Flow At Cart Wall")]
    public static void PlaceInDemoLevel()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path!="Assets/Scenes/Demo Level.unity") throw new InvalidOperationException("Open Demo Level first.");
        var existing=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Climbing Wall Flow - Cart Wall");
        if (!existing)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Climbing Wall Flow.prefab");
            if (!prefab) throw new InvalidOperationException("Build the climbing flow prefab first.");
            existing=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            Undo.RegisterCreatedObjectUndo(existing,"Place climbing wall tutorial");
            existing.name="Climbing Wall Flow - Cart Wall";
            // Left Wall's inner face is x=81. Keep 2 cm clearance from the wall.
            existing.transform.SetPositionAndRotation(new Vector3(81.02f,2.05f,60.25f),Quaternion.Euler(0,-90,0));
            var flow=existing.GetComponent<AlternatingClimbFlow>();
            flow.loop=true;
            flow.loopPause=0;
            PrefabUtility.RecordPrefabInstancePropertyModifications(existing);
            PrefabUtility.RecordPrefabInstancePropertyModifications(existing.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(flow);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        File.WriteAllText("Logs/climbing-placement.txt","PASS: Cart wall tutorial exists in Demo Level at "+existing.transform.position+"; loop enabled; no proximity gate.");
    }

    // Read-only scene report for locating the user's cart and button wall.
    public static void BuildAndInspect()
    {
        Build();
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isBatchMode) scene=EditorSceneManager.OpenScene("Assets/Scenes/Demo Level.unity",OpenSceneMode.Single);
        var report=new System.Text.StringBuilder();
        foreach (var go in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
        {
            string lower=go.name.ToLowerInvariant();
            if (!lower.Contains("cart") && !lower.Contains("button") && !lower.Contains("wall")) continue;
            string path=go.name;
            for(var p=go.parent;p;p=p.parent) path=p.name+"/"+path;
            report.AppendLine(path+" | position="+go.position.ToString("F3")+" rotation="+go.eulerAngles.ToString("F2"));
            foreach(var r in go.GetComponents<Renderer>()) report.AppendLine(" bounds="+r.bounds);
        }
        File.WriteAllText("Logs/climbing-scene-report.txt",report.ToString());
        File.WriteAllText("Logs/climbing-validation.txt","PASS: scripts compiled, shader imported, texture and six-stroke prefab built.");
    }
}
