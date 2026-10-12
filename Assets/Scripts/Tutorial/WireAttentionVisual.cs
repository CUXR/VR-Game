using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Visual-only copies keep the Wire's rigidbody, grab points and colliders fixed.
// Original materials and rendering state are restored on exit, grab or disable.
[DisallowMultipleComponent]
public sealed class WireAttentionVisual : MonoBehaviour
{
    private sealed class Surface
    {
        public Renderer source;
        public MeshRenderer proxy;
        public MeshRenderer glow;
        public bool wasHidden;
        public Material[] materials;
        public Color[] colors;
        public string[] colorProperties;
    }
    private readonly List<Surface> surfaces = new List<Surface>();
    private readonly Dictionary<Transform, Transform> mirrors = new Dictionary<Transform, Transform>();
    private Transform visualRoot;
    private Transform basis;
    private Material glowMaterial;
    private Vector3 pivot;
    private Vector3 approach;
    private bool requested;
    private float weight;
    private double startedAt;
    private Component xrGrab;
    private PropertyInfo xrSelected;
    private Component legacyGrab;
    private PropertyInfo legacyHeld;

    private void Awake()
    {
        xrGrab = GetComponent("XRGrabInteractable");
        xrSelected = xrGrab != null ? xrGrab.GetType().GetProperty("isSelected") : null;
        legacyGrab = GetComponent("Holdable");
        legacyHeld = legacyGrab != null ? legacyGrab.GetType().GetProperty("IsHeld") : null;
        var sources = GetComponentsInChildren<MeshRenderer>(true);
        var glowShader = Resources.Load<Shader>("TutorialDesign/WireAttentionGlow");
        if (glowShader != null) glowMaterial = new Material(glowShader);
        var root = new GameObject("Wire Attention — Visual Only");
        visualRoot = root.transform;
        visualRoot.SetParent(transform, false);
        basis = new GameObject("Original Mesh Coordinates").transform;
        basis.SetParent(visualRoot, false);
        mirrors.Add(transform, basis);
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        foreach (var source in sources)
        {
            if (!source.enabled || !source.gameObject.activeInHierarchy || source.forceRenderingOff
                || !source.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
            if (!hasBounds) { bounds = source.bounds; hasBounds = true; }
            else bounds.Encapsulate(source.bounds);
            var child = new GameObject(source.name + " — Attention", typeof(MeshFilter), typeof(MeshRenderer));
            child.layer = source.gameObject.layer;
            child.transform.SetParent(MirrorTransform(source.transform), false);
            child.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var proxy = child.GetComponent<MeshRenderer>();
            proxy.shadowCastingMode = source.shadowCastingMode;
            proxy.receiveShadows = source.receiveShadows;
            proxy.lightProbeUsage = source.lightProbeUsage;
            proxy.reflectionProbeUsage = source.reflectionProbeUsage;
            MeshRenderer glow = null;
            if (glowMaterial != null)
            {
                var glowObject = new GameObject(source.name + " — Glow", typeof(MeshFilter), typeof(MeshRenderer));
                glowObject.layer = source.gameObject.layer;
                glowObject.transform.SetParent(child.transform, false);
                glowObject.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                glow = glowObject.GetComponent<MeshRenderer>();
                glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                glow.receiveShadows = false;
                var glowSlots = new Material[filter.sharedMesh.subMeshCount];
                for (int i = 0; i < glowSlots.Length; i++) glowSlots[i] = glowMaterial;
                glow.sharedMaterials = glowSlots;
            }
            surfaces.Add(new Surface { source = source, proxy = proxy, glow = glow, wasHidden = source.forceRenderingOff });
        }
        pivot = transform.InverseTransformPoint(bounds.center);
        basis.localPosition = -pivot;
        visualRoot.gameObject.SetActive(false);
    }

    private Transform MirrorTransform(Transform source)
    {
        if (mirrors.TryGetValue(source, out var mirrored)) return mirrored;
        mirrored = new GameObject(source.name + " — Mesh Pose").transform;
        mirrored.SetParent(MirrorTransform(source.parent), false);
        mirrored.SetLocalPositionAndRotation(source.localPosition, source.localRotation);
        mirrored.localScale = source.localScale;
        mirrors.Add(source, mirrored);
        return mirrored;
    }

    private bool IsHeld => (xrGrab != null && xrSelected != null && (bool)xrSelected.GetValue(xrGrab))
        || (legacyGrab != null && legacyHeld != null && (bool)legacyHeld.GetValue(legacyGrab));

    public bool IsProxy(Renderer renderer) => visualRoot != null && renderer.transform.IsChildOf(visualRoot);
    public bool IsManagedSource(Renderer renderer)
    {
        foreach (var surface in surfaces)
            if (surface.source == renderer && !surface.wasHidden) return true;
        return false;
    }

    public void SetVisible(bool visible, Camera camera)
    {
        visible &= camera != null && !IsHeld;
        if (visible && !requested)
        {
            // Capture one small approach target; never follow the moving head.
            approach = (camera.transform.position - transform.TransformPoint(pivot)).normalized * 0.06f;
            if (weight == 0f) startedAt = Time.unscaledTimeAsDouble;
        }
        requested = visible;
    }

    private void LateUpdate() => Animate(Time.unscaledDeltaTime, Time.unscaledTimeAsDouble);

    private void Animate(float deltaTime, double now)
    {
        if (IsHeld) { Restore(); return; }
        weight = Mathf.MoveTowards(weight, requested ? 1f : 0f, deltaTime / 0.85f);
        if (weight == 0f) { Restore(); return; }
        if (!visualRoot.gameObject.activeSelf)
        {
            PrepareMaterials();
            visualRoot.gameObject.SetActive(true);
        }
        // Quintic ease has zero speed and acceleration at both endpoints.
        float eased = weight * weight * weight * (weight * (weight * 6f - 15f) + 10f);
        float breath = Mathf.Sin((float)(now - startedAt) * Mathf.PI * 2f / 4.8f);
        float scale = 1f + (0.2f + 0.04f * breath) * eased;
        Vector3 offset = approach * eased
            + Vector3.up * (breath * 0.015f * eased);
        visualRoot.localPosition = pivot + transform.InverseTransformVector(offset);
        visualRoot.localScale = Vector3.one * scale;
        if (glowMaterial != null) glowMaterial.SetFloat("_GlowStrength", 0.3f * eased);
        // Preserve the full authored hierarchy, including nonuniform scales.
        // Flattening it to a world rotation/lossyScale changes the Wire's shape.
        foreach (var pair in mirrors)
        {
            if (pair.Key == transform || pair.Key == null) continue;
            pair.Value.SetLocalPositionAndRotation(pair.Key.localPosition, pair.Key.localRotation);
            pair.Value.localScale = pair.Key.localScale;
            pair.Value.gameObject.SetActive(pair.Key.gameObject.activeSelf);
        }
        foreach (var surface in surfaces)
        {
            if (surface.source == null) continue;
            surface.source.forceRenderingOff = true;
            surface.proxy.enabled = surface.source.enabled && surface.source.gameObject.activeInHierarchy;
            if (surface.glow != null) surface.glow.enabled = surface.proxy.enabled;
            for (int i = 0; i < surface.materials.Length; i++)
            {
                if (surface.colorProperties[i] == null) continue;
                Color original = surface.colors[i];
                Color bright = Color.Lerp(original * 1.65f, Color.white, 0.18f);
                bright.a = original.a;
                surface.materials[i].SetColor(surface.colorProperties[i], Color.Lerp(original, bright, eased));
            }
        }
    }

    private void PrepareMaterials()
    {
        foreach (var surface in surfaces)
        {
            if (surface.source == null) continue;
            ReleaseMaterials(surface);
            var originals = surface.source.sharedMaterials;
            surface.materials = new Material[originals.Length];
            surface.colors = new Color[originals.Length];
            surface.colorProperties = new string[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                if (originals[i] == null) continue;
                var material = new Material(originals[i]);
                surface.materials[i] = material;
                string property = material.HasProperty("_BaseColor") ? "_BaseColor"
                    : material.HasProperty("_Color") ? "_Color" : null;
                surface.colorProperties[i] = property;
                if (property != null) surface.colors[i] = material.GetColor(property);
            }
            surface.proxy.sharedMaterials = surface.materials;
        }
    }

    private void Restore()
    {
        requested = false;
        weight = 0f;
        if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        foreach (var surface in surfaces)
            if (surface.source != null) surface.source.forceRenderingOff = surface.wasHidden;
    }

    private static void ReleaseMaterials(Surface surface)
    {
        if (surface.materials == null) return;
        foreach (var material in surface.materials)
            if (material != null) ReleaseObject(material);
        surface.materials = null;
    }

    private void OnDisable() => Restore();
    private void OnDestroy()
    {
        Restore();
        foreach (var surface in surfaces) ReleaseMaterials(surface);
        if (glowMaterial != null) ReleaseObject(glowMaterial);
        if (visualRoot != null) ReleaseObject(visualRoot.gameObject);
    }

    private static void ReleaseObject(Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
