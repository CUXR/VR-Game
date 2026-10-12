using System.Collections.Generic;
using UnityEngine;

// Only the headset camera counts. Renderer.isVisible also includes editor and
// other cameras, and cannot be used to decide whether the player sees a Wire.
internal sealed class WireViewVisibility
{
    private struct Target
    {
        public Transform root;
        public Renderer renderer;
    }
    private readonly List<Target> targets = new List<Target>();
    private readonly Plane[] leftPlanes = new Plane[6];
    private readonly Plane[] rightPlanes = new Plane[6];
    private readonly RaycastHit[] hits = new RaycastHit[64];
    private float nextRefresh;

    public void Invalidate() => nextRefresh = float.NegativeInfinity;

    public bool AnyInView(Camera camera, float reachDistance)
        => CollectVisible(camera, reachDistance, null);

    public bool CollectVisible(Camera camera, float reachDistance, List<Transform> visible)
    {
        visible?.Clear();
        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.5f;
            targets.Clear();
            foreach (var wire in GameObject.FindGameObjectsWithTag("Wire"))
            {
                var effect = wire.GetComponent<WireAttentionVisual>();
                foreach (var renderer in wire.GetComponentsInChildren<Renderer>(true))
                    if (effect == null || !effect.IsProxy(renderer))
                        targets.Add(new Target { root = wire.transform, renderer = renderer });
            }
        }
        bool stereo = camera.stereoEnabled;
        if (stereo)
        {
            GeometryUtility.CalculateFrustumPlanes(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left)
                * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left), leftPlanes);
            GeometryUtility.CalculateFrustumPlanes(camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right)
                * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right), rightPlanes);
        }
        else GeometryUtility.CalculateFrustumPlanes(camera, leftPlanes);
        var origin = camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        Transform player = origin != null ? origin.transform : camera.transform;
        Vector3 eye = camera.transform.position;
        float reachSquared = reachDistance * reachDistance;
        foreach (var target in targets)
        {
            var renderer = target.renderer;
            if (renderer == null || target.root == null || !renderer.enabled
                || !renderer.gameObject.activeInHierarchy
                || (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
            var effect = target.root.GetComponent<WireAttentionVisual>();
            if (renderer.forceRenderingOff && (effect == null || !effect.IsManagedSource(renderer))) continue;
            if (visible != null && visible.Contains(target.root)) continue;
            Bounds bounds = renderer.bounds;
            if ((bounds.ClosestPoint(eye) - eye).sqrMagnitude > reachSquared) continue;
            if (!GeometryUtility.TestPlanesAABB(leftPlanes, bounds)
                && !(stereo && GeometryUtility.TestPlanesAABB(rightPlanes, bounds))) continue;
            // Sample the centre and inset face centres. A partly exposed Wire
            // still counts, but a fully blocked one cannot trigger the tutorial.
            for (int sample = 0; sample < 7; sample++)
            {
                Vector3 point = bounds.center;
                if (sample > 0)
                {
                    int axis = (sample - 1) / 2;
                    point[axis] += bounds.extents[axis] * (sample % 2 == 0 ? -0.8f : 0.8f);
                }
                Vector3 direction = point - eye;
                if (direction.sqrMagnitude > reachSquared) continue;
                if (!PointInView(point, leftPlanes)
                    && !(stereo && PointInView(point, rightPlanes))) continue;
                if (Unobstructed(eye, direction, target.root, player))
                {
                    if (visible == null) return true;
                    visible.Add(target.root);
                    break;
                }
            }
        }
        return visible != null && visible.Count > 0;
    }

    private static bool PointInView(Vector3 point, Plane[] planes)
    {
        foreach (var plane in planes)
            if (plane.GetDistanceToPoint(point) < 0f) return false;
        return true;
    }

    private bool Unobstructed(Vector3 eye, Vector3 direction, Transform wire, Transform player)
    {
        float distance = direction.magnitude;
        if (distance < 0.0001f) return true;
        int count = Physics.RaycastNonAlloc(eye, direction / distance, hits,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // Overflow is treated as blocked rather than accidentally seeing through geometry.
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i].transform;
            if (hit.IsChildOf(wire) || hit.IsChildOf(player)) continue;
            return false;
        }
        return true;
    }
}
