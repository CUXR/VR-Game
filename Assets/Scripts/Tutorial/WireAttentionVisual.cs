using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Visual-only copies keep the Wire's rigidbody, grab points and colliders fixed.
// Original materials and rendering state are restored on exit, grab or disable.
[DisallowMultipleComponent]
public sealed class WireAttentionVisual : MonoBehaviour
{
    private sealed class Surface
    {
        public Renderer source;
        public MeshRenderer proxy;
        public bool wasHidden;
        public Material[] materials;
        public Color[] colors;
        public string[] colorProperties;
    }
    private readonly List<Surface> surfaces = new List<Surface>();
    private readonly Dictionary<Transform, Transform> mirrors = new Dictionary<Transform, Transform>();
    private Transform visualRoot;
    private Transform basis;
    private static uint allocatedLightLayers;
    private uint lightLayer;
    private Light topLight;
    private Light fillLight;
    private float lightWeight;
    private Vector3 pivot;
    private Vector3 approach;
    private bool requested;
    private float weight;
    private Component xrGrab;
    private PropertyInfo xrSelected;
    private Component legacyGrab;
    private PropertyInfo legacyHeld;
    private TutorialAttentionStyle settings;
    public void Configure(TutorialAttentionStyle style) { if (style != null) settings = style; }

    private void Awake()
    {
        settings = Resources.Load<TutorialAttentionStyle>("TutorialDesign/Attention Style");
        xrGrab = GetComponent("XRGrabInteractable");
        xrSelected = xrGrab != null ? xrGrab.GetType().GetProperty("isSelected") : null;
        legacyGrab = GetComponent("Holdable");
        legacyHeld = legacyGrab != null ? legacyGrab.GetType().GetProperty("IsHeld") : null;
        var sources = GetComponentsInChildren<MeshRenderer>(true);
        lightLayer = AllocateLightLayer();
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
            proxy.renderingLayerMask = source.renderingLayerMask | lightLayer;
            source.renderingLayerMask |= lightLayer;
            surfaces.Add(new Surface { source = source, proxy = proxy, wasHidden = source.forceRenderingOff });
        }
        pivot = transform.InverseTransformPoint(bounds.center);
        basis.localPosition = -pivot;
        visualRoot.gameObject.SetActive(false);
        if (lightLayer != 0)
        {
            // Independent world transforms preserve the lighting direction as
            // the wire rotates, while positions follow its world-space bounds.
            topLight = new GameObject("Wire — Isolated Key Light", typeof(Light)).GetComponent<Light>();
            topLight.type = LightType.Point;
            topLight.color = Color.white;
            topLight.shadows = LightShadows.None;
            topLight.bounceIntensity = 0f;
            topLight.enabled = false;
            topLight.GetUniversalAdditionalLightData().renderingLayers = lightLayer;
            fillLight = new GameObject("Wire — Isolated Fill Light", typeof(Light)).GetComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = topLight.color;
            fillLight.shadows = LightShadows.None;
            fillLight.bounceIntensity = 0f;
            fillLight.enabled = false;
            fillLight.GetUniversalAdditionalLightData().renderingLayers = lightLayer;
        }
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
            approach = (camera.transform.position - transform.TransformPoint(pivot)).normalized
                * (settings != null ? settings.approachDistance : 0.06f);

        }
        requested = visible;
    }

    private void LateUpdate()
    {
        Animate(Time.unscaledDeltaTime, Time.unscaledTimeAsDouble);
        UpdateTopLight();
    }

    private static uint AllocateLightLayer()
    {
        uint used = allocatedLightLayers | 1u;
        foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            used |= renderer.renderingLayerMask;
        foreach (var light in Object.FindObjectsByType<UniversalAdditionalLightData>(FindObjectsInactive.Include))
            used |= light.renderingLayers;
        // The default URP rendering-layer layout supports eight bits. Reserve
        // a free bit per wire so nearby wires cannot light one another.
        for (int bit = 7; bit >= 1; bit--)
        {
            uint mask = 1u << bit;
            if ((used & mask) != 0) continue;
            allocatedLightLayers |= mask;
            return mask;
        }
        Debug.LogWarning("No unused rendering layer is available for the isolated wire light.");
        return 0;
    }

    private void UpdateTopLight()
    {
        if (topLight == null) return;
        lightWeight = Mathf.MoveTowards(lightWeight, requested || IsHeld ? 1f : 0f, Time.unscaledDeltaTime / 0.3f);
        bool found = false;
        Bounds bounds = default;
        foreach (var surface in surfaces)
        {
            var renderer = visualRoot.gameObject.activeSelf ? surface.proxy : surface.source;
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        topLight.enabled = found && lightWeight > 0f;
        fillLight.enabled = topLight.enabled;
        if (!topLight.enabled) return;
        float distance = Mathf.Max(0.4f, bounds.extents.magnitude * 2.5f);
        Vector3 direction = new Vector3(-1f, 1.3f, -1f).normalized;
        topLight.transform.SetPositionAndRotation(bounds.center + direction * distance, Quaternion.identity);
        fillLight.transform.SetPositionAndRotation(bounds.center - direction * distance, Quaternion.identity);
        topLight.range = fillLight.range = distance * 3f;
        float breath = 0.5f - 0.5f * Mathf.Cos(Time.realtimeSinceStartup * Mathf.PI * 2f / 2.4f);
        // Compensate for inverse-square attenuation when wire dimensions differ.
        float strength = distance * distance * lightWeight * Mathf.Lerp(0.85f, 1f, breath);
        topLight.intensity = 0.45f * strength;
        fillLight.intensity = 0.22f * strength;
    }

    private void Animate(float deltaTime, double now)
    {
        if (IsHeld) { Restore(); return; }
        weight = Mathf.MoveTowards(weight, requested ? 1f : 0f,
            deltaTime / (settings != null ? settings.transitionDuration : 0.85f));
        if (weight == 0f) { Restore(); return; }
        if (!visualRoot.gameObject.activeSelf)
        {
            PrepareMaterials();
            visualRoot.gameObject.SetActive(true);
        }
        // Quintic ease has zero speed and acceleration at both endpoints.
        float eased = weight * weight * weight * (weight * (weight * 6f - 15f) + 10f);
        float breath = 0.5f - 0.5f * Mathf.Cos(Time.realtimeSinceStartup * Mathf.PI * 2f / 2.4f);
        float scale = 1f + ((settings != null ? settings.scale - 1f : 0.2f)
            + (settings != null ? settings.scaleBreath : 0.04f) * breath) * eased;
        Vector3 offset = approach * eased
            + Vector3.up * (breath * (settings != null ? settings.floatAmplitude : 0.015f) * eased);
        visualRoot.localPosition = pivot + transform.InverseTransformVector(offset);
        visualRoot.localScale = Vector3.one * scale;
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
            for (int i = 0; i < surface.materials.Length; i++)
            {
                if (surface.colorProperties[i] == null) continue;
                Color original = surface.colors[i];
                surface.materials[i].SetColor(surface.colorProperties[i], original);
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

    private void OnDisable()
    {
        Restore();
        lightWeight = 0f;
        if (topLight != null) topLight.enabled = false;
        if (fillLight != null) fillLight.enabled = false;
    }
    private void OnDestroy()
    {
        Restore();
        foreach (var surface in surfaces)
            if (surface.source != null) surface.source.renderingLayerMask &= ~lightLayer;
        allocatedLightLayers &= ~lightLayer;
        if (topLight != null) ReleaseObject(topLight.gameObject);
        if (fillLight != null) ReleaseObject(fillLight.gameObject);

        foreach (var surface in surfaces) ReleaseMaterials(surface);
        if (visualRoot != null) ReleaseObject(visualRoot.gameObject);
    }

    private static void ReleaseObject(Object value)
    {
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
