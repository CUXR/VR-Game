using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;

// Generates only the tutorial's controller visual. No scene, camera, XR setting,
// interaction component or team prefab is modified.
public sealed class TutorialTrackedControllerImporter : IPreprocessBuildWithReport
{
    private const string ModelPath = "Assets/TutorialDesign/Models/MetaQuestTouchPlus_Left.fbx";
    private const string PrefabPath = "Assets/TutorialDesign/Prefabs/Tracked Left Touch Plus.prefab";
    private const string MeshPath = "Assets/TutorialDesign/Models/TouchPlusLeftAnimated.asset";
    private const string StylePath = "Assets/Resources/TutorialDesign/Curved Tutorial Panel Style.asset";
    public int callbackOrder => -100;

    [InitializeOnLoadMethod]
    private static void SchedulePreparation() { EditorApplication.delayCall += PrepareIfMissing; }

    private static void PrepareIfMissing()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += PrepareIfMissing;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            Build();
        else ConnectStyle();
    }

    public void OnPreprocessBuild(BuildReport report) { PrepareIfMissing(); }

    [MenuItem("VR Game/UI/Rebuild Tracked Left Controller")]
    public static void Build()
    {
        var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (imported == null) throw new InvalidOperationException("Official left Touch Plus FBX is missing.");
        var clip = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null) throw new InvalidOperationException("Official controller button animation is missing.");
        var root = new GameObject("Tracked Left Touch Plus");
        try
        {
            var model = UnityEngine.Object.Instantiate(imported, root.transform, false);
            model.name = "Official Touch Plus Left Rig";
            var bindings = AnimationUtility.GetCurveBindings(clip);
            float firstKey = bindings.Select(b => AnimationUtility.GetEditorCurve(clip, b))
                .Where(c => c != null && c.length > 0).Min(c => c.keys[0].time);
            Action<int> sample = frame => clip.SampleAnimation(model, firstKey + (frame - 1) / clip.frameRate);
            sample(1);
            var bones = model.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
            var motions = new List<TutorialControllerInputAnimation.BoneMotion>();
            AddMotion(motions, bones["b_button_x"], TutorialControllerInputAnimation.Control.X, sample, 6);
            AddMotion(motions, bones["b_button_y"], TutorialControllerInputAnimation.Control.Y, sample, 10);
            AddMotion(motions, bones["left_b_button_oculus"], TutorialControllerInputAnimation.Control.Menu, sample, 30);
            AddMotion(motions, bones["left_b_trigger_front"], TutorialControllerInputAnimation.Control.Trigger, sample, 20);
            AddMotion(motions, bones["left_b_trigger_grip"], TutorialControllerInputAnimation.Control.Grip, sample, 26);
            AddMotion(motions, bones["left_b_thumbstick"], TutorialControllerInputAnimation.Control.StickUp, sample, 38);
            AddMotion(motions, bones["left_b_thumbstick"], TutorialControllerInputAnimation.Control.StickDown, sample, 36);
            AddMotion(motions, bones["left_b_thumbstick"], TutorialControllerInputAnimation.Control.StickLeft, sample, 40);
            AddMotion(motions, bones["left_b_thumbstick"], TutorialControllerInputAnimation.Control.StickRight, sample, 42);
            sample(1);
            foreach (var animator in model.GetComponentsInChildren<Animator>())
                UnityEngine.Object.DestroyImmediate(animator);

            var body = AssetDatabase.LoadAssetAtPath<Material>("Assets/TutorialDesign/Materials/Controller Body Transparent.mat");
            var white = AssetDatabase.LoadAssetAtPath<Material>("Assets/TutorialDesign/Materials/Controller Side Grip White.mat");
            var glowMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/TutorialDesign/Materials/Controller Side Grip Glow.mat");
            if (body == null || white == null || glowMaterial == null)
                throw new InvalidOperationException("Tutorial controller materials are missing.");
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var main = skins.Single(s => s.bones.Any(b => b.name == "left_b_trigger_grip"));
            int gripIndex = Array.FindIndex(main.bones, b => b.name == "left_b_trigger_grip");
            var weights = main.sharedMesh.boneWeights;
            var triangles = main.sharedMesh.triangles;
            var bodyTriangles = new List<int>();
            var gripTriangles = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                float weight = 0f;
                for (int j = 0; j < 3; j++) weight += WeightFor(weights[triangles[i + j]], gripIndex);
                var part = weight > 1.5f ? gripTriangles : bodyTriangles;
                part.Add(triangles[i]); part.Add(triangles[i + 1]); part.Add(triangles[i + 2]);
            }
            if (gripTriangles.Count == 0) throw new InvalidOperationException("No side grip geometry was found.");
            var mesh = UnityEngine.Object.Instantiate(main.sharedMesh);
            mesh.name = "Touch Plus Left Animated";
            mesh.subMeshCount = 2;
            mesh.SetTriangles(bodyTriangles, 0);
            mesh.SetTriangles(gripTriangles, 1);
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, MeshPath); savedMesh = mesh; }
            else { EditorUtility.CopySerialized(mesh, savedMesh); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(savedMesh); }
            main.sharedMesh = savedMesh;
            main.sharedMaterials = new[] { body, white };
            foreach (var skin in skins)
            {
                if (skin != main) skin.sharedMaterial = body;
                skin.shadowCastingMode = ShadowCastingMode.Off;
                skin.receiveShadows = false;
                skin.lightProbeUsage = LightProbeUsage.Off;
                skin.reflectionProbeUsage = ReflectionProbeUsage.Off;
                skin.updateWhenOffscreen = true;
            }

            var baked = new Mesh();
            Vector3 gripCentre;
            try
            {
                main.BakeMesh(baked);
                var vertices = baked.vertices;
                var indices = gripTriangles.Distinct().ToArray();
                gripCentre = indices.Aggregate(Vector3.zero,
                    (sum, index) => sum + main.transform.TransformPoint(vertices[index])) / indices.Length;
            }
            finally { UnityEngine.Object.DestroyImmediate(baked); }
            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = "Side grip glow";
            UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.transform.SetParent(bones["left_b_trigger_grip"], false);
            glow.transform.position = gripCentre;
            float scale = Mathf.Max(0.00001f, glow.transform.parent.lossyScale.x);
            glow.transform.localScale = Vector3.one * 0.035f / scale;
            var glowRenderer = glow.GetComponent<MeshRenderer>();
            glowRenderer.sharedMaterial = glowMaterial;
            glowRenderer.shadowCastingMode = ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;

            var driver = root.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.positionInput = new InputActionProperty(new InputAction("Left Grip Position", InputActionType.Value,
                "<XRController>{LeftHand}/devicePosition", expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(new InputAction("Left Grip Rotation", InputActionType.Value,
                "<XRController>{LeftHand}/deviceRotation", expectedControlType: "Quaternion"));
            driver.trackingStateInput = new InputActionProperty(new InputAction("Left Tracking State", InputActionType.Value,
                "<XRController>{LeftHand}/trackingState", expectedControlType: "Integer"));
            root.AddComponent<TutorialHandControllerVisual>();
            root.AddComponent<TutorialControllerInputAnimation>().Configure(motions.ToArray(), glow.transform);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssetIfDirty(savedMesh);
            ConnectStyle();
            TutorialControllerModelImporter.CheckAndroidControllerConfiguration();
            Debug.Log("TUTORIAL_TRACKED_CONTROLLER_READY: metre-scale official rig, TrackedPoseDriver, X/Y/trigger/grip/thumbstick input animations.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void ConnectStyle()
    {
        var style = AssetDatabase.LoadAssetAtPath<TutorialPanelStyle>(StylePath);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (style == null || prefab == null || style.handControllerPrefab == prefab) return;
        style.handControllerPrefab = prefab;
        EditorUtility.SetDirty(style);
        AssetDatabase.SaveAssetIfDirty(style);
    }

    private static void AddMotion(List<TutorialControllerInputAnimation.BoneMotion> motions, Transform bone,
        TutorialControllerInputAnimation.Control control, Action<int> sample, int pressedFrame)
    {
        sample(1);
        var motion = new TutorialControllerInputAnimation.BoneMotion
        { bone = bone, control = control, restPosition = bone.localPosition, restRotation = bone.localRotation };
        sample(pressedFrame);
        motion.pressedPosition = bone.localPosition;
        motion.pressedRotation = bone.localRotation;
        motions.Add(motion);
    }

    private static float WeightFor(BoneWeight w, int bone)
        => (w.boneIndex0 == bone ? w.weight0 : 0f) + (w.boneIndex1 == bone ? w.weight1 : 0f)
            + (w.boneIndex2 == bone ? w.weight2 : 0f) + (w.boneIndex3 == bone ? w.weight3 : 0f);
}
