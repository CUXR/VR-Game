using UnityEngine;

/// <summary>Moves the entire tutorial with the cart, gated by a fixed world-space zone.</summary>
[DisallowMultipleComponent]
public sealed class CartClimbTutorialZone : MonoBehaviour
{
    public Transform cart;
    public Vector3 zoneCenter;
    public Quaternion cartReferenceRotation = Quaternion.identity;
    [Tooltip("Direct children that follow the cart horizontally but stay on the authored flat floor.")]
    public Transform[] groundElements = System.Array.Empty<Transform>();
    public Vector3 zoneHalfExtents = new Vector3(0.75f, 0.5f, 0.75f);
    [Min(0)] public float reentryInset = 0.08f;

    Renderer[] visuals;
    bool[] originalHidden;
    Vector3 localPosition, originalPosition;
    Quaternion localRotation, originalRotation;
    bool visible;
    Vector3[] groundPositions, groundLocalPositions;
    Quaternion[] groundRotations, groundLocalRotations;

    void OnEnable()
    {
        visuals = GetComponentsInChildren<Renderer>(true);
        originalHidden = new bool[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
            originalHidden[i] = visuals[i].forceRenderingOff;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        groundPositions = new Vector3[groundElements.Length];
        groundRotations = new Quaternion[groundElements.Length];
        groundLocalPositions = new Vector3[groundElements.Length];
        groundLocalRotations = new Quaternion[groundElements.Length];
        for (int i = 0; i < groundElements.Length; i++)
        {
            var element = groundElements[i];
            if (!element) continue;
            groundPositions[i] = element.position;
            groundRotations[i] = element.rotation;
            groundLocalPositions[i] = element.localPosition;
            groundLocalRotations[i] = element.localRotation;
        }
        if (cart)
        {
            // Use the authored zone pose, even if another script moved the cart
            // before this component enabled. Do not resize the tutorial with the cart.
            localPosition = Quaternion.Inverse(cartReferenceRotation) * (transform.position - zoneCenter);
            localRotation = Quaternion.Inverse(cartReferenceRotation) * transform.rotation;
        }
        visible = true;
        LateUpdate();
    }

    void LateUpdate()
    {
        if (cart)
        {
            transform.SetPositionAndRotation(cart.position + cart.rotation * localPosition, cart.rotation * localRotation);
            var referenceForward = Vector3.ProjectOnPlane(cartReferenceRotation * Vector3.forward, Vector3.up);
            var forward = Vector3.ProjectOnPlane(cart.forward, Vector3.up);
            var yaw = referenceForward.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f
                ? Quaternion.AngleAxis(Vector3.SignedAngle(referenceForward, forward, Vector3.up), Vector3.up)
                : Quaternion.identity;
            var translation = Vector3.ProjectOnPlane(cart.position - zoneCenter, Vector3.up);
            for (int i = 0; i < groundElements.Length; i++)
            {
                if (!groundElements[i]) continue;
                var position = zoneCenter + yaw * (groundPositions[i] - zoneCenter) + translation;
                position.y = groundPositions[i].y;
                groundElements[i].SetPositionAndRotation(position, yaw * groundRotations[i]);
            }
        }
        Vector3 d = cart ? cart.position - zoneCenter : Vector3.positiveInfinity;
        float inset = visible ? 0 : reentryInset;
        visible = cart && Mathf.Abs(d.x) <= Mathf.Max(0, zoneHalfExtents.x - inset)
            && Mathf.Abs(d.y) <= Mathf.Max(0, zoneHalfExtents.y - inset)
            && Mathf.Abs(d.z) <= Mathf.Max(0, zoneHalfExtents.z - inset);
        if (visuals == null) return;
        for (int i = 0; i < visuals.Length; i++)
            if (visuals[i]) visuals[i].forceRenderingOff = originalHidden[i] || !visible;
    }

    void OnDisable()
    {
        if (visuals != null)
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i]) visuals[i].forceRenderingOff = originalHidden[i];
        transform.SetPositionAndRotation(originalPosition, originalRotation);
        if (groundLocalPositions != null)
            for (int i = 0; i < groundLocalPositions.Length; i++)
                if (groundElements[i]) groundElements[i].SetLocalPositionAndRotation(groundLocalPositions[i], groundLocalRotations[i]);
    }
}
