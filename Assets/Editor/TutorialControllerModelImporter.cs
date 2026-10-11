using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;

public static class TutorialControllerModelImporter
{
    private const string ModelPath = "Assets/TutorialDesign/Models/MetaQuestTouchPlus_Left.fbx";
    private const string PrefabPath = "Assets/TutorialDesign/Prefabs/Rotating Controller Display.prefab";
    private const string MeshFolder = "Assets/TutorialDesign/Models/";

    [MenuItem("VR Game/UI/Rebuild Controller Display")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (imported == null) throw new InvalidOperationException("Touch Plus left controller is missing.");
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        var source = UnityEngine.Object.Instantiate(imported);
        try
        {
            var oldModel = prefab.transform.GetChild(0);
            var oldPress = oldModel.GetComponentInChildren<TutorialButtonPressAnimation>();
            if (oldPress == null) throw new InvalidOperationException("Existing grip animation is missing.");
            var oldGlow = oldModel.GetComponentsInChildren<MeshRenderer>().Single(r => r.name == "Side grip glow");
            var oldGrip = oldPress.GetComponent<MeshRenderer>();
            var oldBody = oldModel.GetComponentsInChildren<MeshRenderer>().First(r => r != oldGrip && r != oldGlow);
            var oldBounds = GetBounds(oldModel.GetComponentsInChildren<Renderer>().Where(r => r != oldGlow));
            var animationJson = JsonUtility.ToJson(oldPress);
            var bodyMaterial = oldBody.sharedMaterial;
            var buttonMaterial = oldGrip.sharedMaterial;
            var glowMaterial = oldGlow.sharedMaterial;
            float glowSize = oldGlow.transform.lossyScale.x;

            var skins = source.GetComponentsInChildren<SkinnedMeshRenderer>();
            var mainSkin = skins.Single(s => s.bones.Any(b => b.name.EndsWith("trigger_grip")));
            int gripBone = Array.FindIndex(mainSkin.bones, b => b.name.EndsWith("trigger_grip"));
            var visual = new GameObject("Meta Quest Touch Plus Left");
            visual.transform.SetParent(prefab.transform, false);
            var generated = new List<MeshRenderer>();
            MeshRenderer newGrip = null;
            Vector3 stickCentre = Vector3.zero;
            foreach (var skin in skins)
            {
                var baked = new Mesh();
                skin.BakeMesh(baked, false);
                try
                {
                    Matrix4x4 toModel = source.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                    var vertices = baked.vertices.Select(toModel.MultiplyPoint3x4).ToArray();
                    var normals = baked.normals.Select(n => toModel.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                    var bodyIndices = new List<int>();
                    var gripIndices = new List<int>();
                    var weights = skin.sharedMesh.boneWeights;
                    var indices = baked.triangles;
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        float gripWeight = 0f;
                        if (skin == mainSkin)
                            for (int j = 0; j < 3; j++) gripWeight += WeightFor(weights[indices[t + j]], gripBone);
                        var destination = gripWeight > 1.5f ? gripIndices : bodyIndices;
                        destination.Add(indices[t]); destination.Add(indices[t + 1]); destination.Add(indices[t + 2]);
                    }
                    if (skin == mainSkin)
                    {
                        int stickBone = Array.FindIndex(skin.bones, b => b.name.EndsWith("thumbstick"));
                        var stickPoints = vertices.Where((v, i) => WeightFor(weights[i], stickBone) > 0.5f).ToArray();
                        if (stickPoints.Length == 0) throw new InvalidOperationException("Thumbstick geometry is missing.");
                        stickCentre = stickPoints.Aggregate(Vector3.zero, (a, b) => a + b) / stickPoints.Length;
                    }
                    if (bodyIndices.Count > 0)
                        generated.Add(CreatePart(visual.transform, skin == mainSkin ? "Touch Plus body" : "Touch Plus battery indicator",
                            SaveMesh(skin == mainSkin ? "TouchPlusLeftBody.asset" : "TouchPlusLeftIndicator.asset", baked, vertices, normals, bodyIndices), bodyMaterial));
                    if (gripIndices.Count > 0)
                    {
                        newGrip = CreatePart(visual.transform, "Side grip button",
                            SaveMesh("TouchPlusLeftGrip.asset", baked, vertices, normals, gripIndices), buttonMaterial);
                        generated.Add(newGrip);
                        Debug.Log("TOUCH_PLUS_GRIP_TRIANGLES=" + gripIndices.Count / 3);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            if (newGrip == null) throw new InvalidOperationException("No grip-bound triangles found in the official controller.");
            var newBounds = GetBounds(generated);
            Vector3 sourceUp = (stickCentre - newBounds.center).normalized;
            Vector3 sourceFace = Vector3.ProjectOnPlane(Vector3.up, sourceUp).normalized;
            // Use the original tilt direction, increased to a 45-degree roll.
            // Keep the existing depth tilt and the manufacturer's axis conversion.
            visual.transform.localRotation = Quaternion.AngleAxis(45f, Vector3.forward)
                * Quaternion.AngleAxis(-20f, Vector3.right)
                * Quaternion.LookRotation(Vector3.back, Vector3.up)
                * Quaternion.Inverse(Quaternion.LookRotation(sourceFace, sourceUp));
            newBounds = GetBounds(generated);
            visual.transform.localScale = Vector3.one * (oldBounds.size.y / newBounds.size.y);
            newBounds = GetBounds(generated);
            visual.transform.position += oldBounds.center - newBounds.center;

            var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            halo.name = "Side grip glow";
            UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());
            halo.transform.SetParent(newGrip.transform, false);
            halo.transform.position = newGrip.bounds.center;
            halo.transform.rotation = oldGlow.transform.rotation;
            halo.transform.localScale = Vector3.one * glowSize / Mathf.Abs(newGrip.transform.lossyScale.x);
            ConfigureRenderer(halo.GetComponent<MeshRenderer>(), glowMaterial);
            var press = newGrip.gameObject.AddComponent<TutorialButtonPressAnimation>();
            JsonUtility.FromJsonOverwrite(animationJson, press);
            Vector3 inward = Vector3.right;
            Vector3 bodyCentreLocal = visual.transform.InverseTransformPoint(GetBounds(generated.Where(r => r != newGrip)).center);
            if (Vector3.Dot(inward, bodyCentreLocal
                - newGrip.GetComponent<MeshFilter>().sharedMesh.bounds.center) < 0f) inward = -inward;
            press.SetDirectionAndGlow(inward, halo.transform);

            UnityEngine.Object.DestroyImmediate(oldModel.gameObject);
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            Debug.Log("TOUCH_PLUS_REPLACED: height=" + GetBounds(generated).size.y
                + "; centre=" + GetBounds(generated).center
                + "; preserved body, button and glow materials, animation settings and prefab root.");
            CheckAndroidControllerConfiguration();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    public static void CheckAndroidControllerConfiguration()
    {
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        bool touchEnabled = settings != null && settings.GetFeatures().Any(feature => feature != null
            && feature.enabled && (feature.GetType().Name == "OculusTouchControllerProfile"
                || feature.GetType().Name == "MetaQuestTouchPlusControllerProfile"
                || feature.GetType().Name == "MetaQuestTouchProControllerProfile"));
        bool questEnabled = settings != null && settings.GetFeatures().Any(feature => feature != null
            && feature.enabled && feature.GetType().Name == "MetaQuestFeature");
        if (touchEnabled && questEnabled)
            Debug.Log("CONTROLLER_IMPORT_CHECK: Android Touch input and Meta Quest Support are enabled.");
        else
            Debug.LogError("CONTROLLER_IMPORT_CHECK: Android Touch input=" + touchEnabled
                + "; Meta Quest Support=" + questEnabled
                + ". Check Android OpenXR settings before testing the UI button.");
    }

    private static float WeightFor(BoneWeight weight, int bone)
    {
        if (bone < 0) return 0f;
        return (weight.boneIndex0 == bone ? weight.weight0 : 0f)
            + (weight.boneIndex1 == bone ? weight.weight1 : 0f)
            + (weight.boneIndex2 == bone ? weight.weight2 : 0f)
            + (weight.boneIndex3 == bone ? weight.weight3 : 0f);
    }

    private static Mesh SaveMesh(string name, Mesh baked, Vector3[] vertices, Vector3[] normals, List<int> indices)
    {
        // Compact each part so its bounds contain only its own geometry.
        var used = indices.Distinct().ToArray();
        var remap = used.Select((index, compact) => new { index, compact }).ToDictionary(p => p.index, p => p.compact);
        var mesh = new Mesh { name = name.Replace(".asset", "") };
        mesh.vertices = used.Select(i => vertices[i]).ToArray();
        mesh.normals = used.Select(i => normals[i]).ToArray();
        var uv = baked.uv;
        if (uv.Length == vertices.Length) mesh.uv = used.Select(i => uv[i]).ToArray();
        mesh.triangles = indices.Select(i => remap[i]).ToArray();
        mesh.RecalculateBounds();
        var path = MeshFolder + name;
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(existing); }
        AssetDatabase.SaveAssetIfDirty(mesh);
        return mesh;
    }

    private static MeshRenderer CreatePart(Transform parent, string name, Mesh mesh, Material material)
    {
        var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(parent, false);
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.GetComponent<MeshRenderer>();
        ConfigureRenderer(renderer, material);
        return renderer;
    }

    private static void ConfigureRenderer(MeshRenderer renderer, Material material)
    {
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Bounds GetBounds(IEnumerable<Renderer> renderers)
    {
        var items = renderers.ToArray();
        var bounds = items[0].bounds;
        foreach (var renderer in items.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}

