using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Authoring only: bake real mesh/button boundaries, never triangle wireframes or billboard halos.</summary>
public static class TutorialControllerGlassMaskImporter
{
    private const string ModelPath = "Assets/TutorialDesign/Models/MetaQuestTouchPlus_Left.fbx";
    private const string BodyPath = "Assets/TutorialDesign/Materials/Controller Body Transparent.mat";
    private const string MaskPath = "Assets/TutorialDesign/Textures/Controller Boundary Distance.png";
    private const string Version = "Tutorial controller boundary distance v2";
    private const int Resolution = 1024;
    private const float DistanceRange = 24f;

    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        EditorApplication.delayCall += Prepare;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Prepare;
        };
    }

    private static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            EditorApplication.delayCall += Prepare;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath);
        var importer = AssetImporter.GetAtPath(MaskPath);
        if (texture == null || importer == null || importer.userData != Version) Bake();
        else Connect(texture);
    }

    [MenuItem("VR Game/UI/Rebuild Controller Glass Boundaries")]
    public static void Bake()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException("Tutorial Touch Plus model is missing.");
        var skin = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Single(renderer => renderer.bones.Any(bone => bone.name == "left_b_trigger_grip"));
        Mesh mesh = skin.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;
        BoneWeight[] weights = mesh.boneWeights;
        if (uvs.Length != vertices.Length || weights.Length != vertices.Length)
            throw new InvalidOperationException("Tutorial mesh needs UVs and controller bone weights.");
        int[] buttonLabels = skin.bones.Select((bone, index) =>
            bone.name.Contains("button") || bone.name.Contains("trigger") || bone.name.Contains("thumbstick") ? index + 1 : 0).ToArray();
        var edges = new Dictionary<EdgeKey, List<EdgeUse>>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
            int labelA = Label(weights[a], buttonLabels), labelB = Label(weights[b], buttonLabels), labelC = Label(weights[c], buttonLabels);
            int label = labelA == labelB || labelA == labelC ? labelA : labelB == labelC ? labelB : 0;
            Add(edges, vertices, uvs, a, b, normal, label);
            Add(edges, vertices, uvs, b, c, normal, label);
            Add(edges, vertices, uvs, c, a, normal, label);
        }

        var distances = Enumerable.Repeat(DistanceRange, Resolution * Resolution).ToArray();
        var priorities = new float[distances.Length];
        int featureCount = 0, buttonFeatures = 0;
        foreach (List<EdgeUse> uses in edges.Values)
        {
            bool button = uses.Any(use => use.label > 0);
            bool boundary = uses.Count == 1 || uses.Any(use => use.label != uses[0].label);
            float cosine = Mathf.Cos((button ? 22f : 35f) * Mathf.Deg2Rad);
            bool crease = uses.Any(use => Vector3.Dot(use.normal, uses[0].normal) < cosine);
            if (!boundary && !crease) continue;
            featureCount++;
            if (button) buttonFeatures++;
            // Rasterize every UV copy of the same geometric edge. UV seams
            // alone do not count as features and cannot create false white lines.
            foreach (EdgeUse use in uses)
                Rasterize(use.a, use.b, button ? .85f : .55f, distances, priorities);
        }
        if (buttonFeatures == 0) throw new InvalidOperationException("No real controller button boundaries were found.");

        var pixels = new Color32[distances.Length];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32((byte)Mathf.RoundToInt(distances[i] / DistanceRange * 255),
                (byte)Mathf.RoundToInt(priorities[i] * 255), 0, 255);
        var texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
        try
        {
            texture.SetPixels32(pixels);
            texture.Apply(false);
            Directory.CreateDirectory(Path.GetDirectoryName(MaskPath));
            File.WriteAllBytes(MaskPath, texture.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        AssetDatabase.ImportAsset(MaskPath, ImportAssetOptions.ForceSynchronousImport);
        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(MaskPath);
        textureImporter.sRGBTexture = false;
        textureImporter.mipmapEnabled = true;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
        textureImporter.filterMode = FilterMode.Trilinear;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.maxTextureSize = Resolution;
        textureImporter.isReadable = false;
        textureImporter.userData = Version;
        textureImporter.SaveAndReimport();
        Connect(AssetDatabase.LoadAssetAtPath<Texture2D>(MaskPath));
        TutorialControllerModelImporter.CheckAndroidControllerConfiguration();
        Debug.Log($"TUTORIAL_CONTROLLER_GLASS_MASK_READY: {featureCount} real edges, {buttonFeatures} button edges, shared {Resolution}px mask.");
    }

    private static void Connect(Texture2D texture)
    {
        Material body = AssetDatabase.LoadAssetAtPath<Material>(BodyPath);
        if (body == null || body.GetTexture("_EdgeMask") == texture) return;
        body.SetTexture("_EdgeMask", texture);
        EditorUtility.SetDirty(body);
        AssetDatabase.SaveAssetIfDirty(body);
    }

    private static int Label(BoneWeight weight, int[] labels)
    {
        float strongest = weight.weight0;
        int index = weight.boneIndex0;
        if (weight.weight1 > strongest) { strongest = weight.weight1; index = weight.boneIndex1; }
        if (weight.weight2 > strongest) { strongest = weight.weight2; index = weight.boneIndex2; }
        if (weight.weight3 > strongest) { strongest = weight.weight3; index = weight.boneIndex3; }
        return strongest > .5f && index < labels.Length ? labels[index] : 0;
    }

    private static void Add(Dictionary<EdgeKey, List<EdgeUse>> edges, Vector3[] positions, Vector2[] uvs,
        int a, int b, Vector3 normal, int label)
    {
        var key = new EdgeKey(positions[a], positions[b]);
        if (!edges.TryGetValue(key, out List<EdgeUse> uses)) edges.Add(key, uses = new List<EdgeUse>(2));
        uses.Add(new EdgeUse { a = uvs[a], b = uvs[b], normal = normal, label = label });
    }

    private static void Rasterize(Vector2 uvA, Vector2 uvB, float priority, float[] distances, float[] priorities)
    {
        Vector2 a = uvA * Resolution - Vector2.one * .5f;
        Vector2 b = uvB * Resolution - Vector2.one * .5f;
        Vector2 direction = b - a;
        float inverseLength = 1f / Mathf.Max(direction.sqrMagnitude, 1e-8f);
        int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - DistanceRange));
        int right = Mathf.Min(Resolution - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + DistanceRange));
        int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - DistanceRange));
        int top = Mathf.Min(Resolution - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + DistanceRange));
        for (int y = bottom; y <= top; y++)
            for (int x = left; x <= right; x++)
            {
                Vector2 point = new Vector2(x, y);
                float t = Mathf.Clamp01(Vector2.Dot(point - a, direction) * inverseLength);
                float distance = Vector2.Distance(point, a + t * direction);
                int pixel = y * Resolution + x;
                if (distance < distances[pixel]) { distances[pixel] = distance; priorities[pixel] = priority; }
                else if (Mathf.Abs(distance - distances[pixel]) < .25f) priorities[pixel] = Mathf.Max(priorities[pixel], priority);
            }
    }

    private struct EdgeUse { internal Vector2 a, b; internal Vector3 normal; internal int label; }

    private readonly struct PointKey : IEquatable<PointKey>, IComparable<PointKey>
    {
        private readonly int x, y, z;
        internal PointKey(Vector3 p) { x = Mathf.RoundToInt(p.x * 10000); y = Mathf.RoundToInt(p.y * 10000); z = Mathf.RoundToInt(p.z * 10000); }
        public bool Equals(PointKey other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object other) => other is PointKey key && Equals(key);
        public override int GetHashCode() => unchecked((x * 397 ^ y) * 397 ^ z);
        public int CompareTo(PointKey other) { int c = x.CompareTo(other.x); if (c == 0) c = y.CompareTo(other.y); return c == 0 ? z.CompareTo(other.z) : c; }
    }

    private readonly struct EdgeKey : IEquatable<EdgeKey>
    {
        private readonly PointKey a, b;
        internal EdgeKey(Vector3 p, Vector3 q)
        {
            var first = new PointKey(p); var second = new PointKey(q);
            a = first.CompareTo(second) <= 0 ? first : second;
            b = first.CompareTo(second) <= 0 ? second : first;
        }
        public bool Equals(EdgeKey other) => a.Equals(other.a) && b.Equals(other.b);
        public override bool Equals(object other) => other is EdgeKey key && Equals(key);
        public override int GetHashCode() => unchecked(a.GetHashCode() * 397 ^ b.GetHashCode());
    }
}
