using UnityEngine;
using UnityEngine.XR;

// Read the existing XR devices. The tutorial does not create or modify the XR rig.
public static class TutorialLeftControllerTracking
{
    public static float Grip()
    {
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (left.TryGetFeatureValue(CommonUsages.grip, out float grip)) return grip;
        return left.TryGetFeatureValue(CommonUsages.gripButton, out bool pressed) && pressed ? 1f : 0f;
    }

    public static bool TryGetWorldPose(out Pose pose)
    {
        pose = default;
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!left.isValid) return false;
        if (left.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && !tracked) return false;
        if (!left.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPosition)
            || !left.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion leftRotation)) return false;
        if (!TryGetTrackingOrigin(out Pose origin)) return false;
        pose = new Pose(origin.position + origin.rotation * leftPosition, origin.rotation * leftRotation);
        return true;
    }

    public static bool TryGetTrackingOrigin(out Pose pose)
    {
        pose = default;
        var camera = Camera.main;
        if (camera == null) return false;
        // Reuse the team's tracking space when an XR Origin is present.
        var xrOrigin = camera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && xrOrigin.CameraFloorOffsetObject != null)
        {
            var offset = xrOrigin.CameraFloorOffsetObject.transform;
            pose = new Pose(offset.position, offset.rotation);
            return true;
        }
        var driver = camera.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (driver != null && camera.transform.parent != null)
        {
            var origin = camera.transform.parent;
            pose = new Pose(origin.position, origin.rotation);
            return true;
        }
        // The current level uses a legacy XR camera. Adapt its existing origin;
        // never add another camera or change the team's rig.
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!head.isValid) return false;
        if (!head.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 headPosition)
            && !head.TryGetFeatureValue(CommonUsages.devicePosition, out headPosition)) return false;
        if (!head.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion headRotation)
            && !head.TryGetFeatureValue(CommonUsages.deviceRotation, out headRotation)) return false;

        Vector3 worldHeadPosition = camera.transform.position;
        Quaternion worldHeadRotation = camera.transform.rotation;
        if (camera.stereoEnabled)
        {
            // This also handles cameras whose XR pose is applied by rendering rather
            // than a Tracked Pose Driver. Using the camera parent alone causes offsets.
            Matrix4x4 leftEye = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse;
            Matrix4x4 rightEye = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
            worldHeadPosition = ((Vector3)leftEye.GetColumn(3) + (Vector3)rightEye.GetColumn(3)) * 0.5f;
            worldHeadRotation = Quaternion.LookRotation(-(Vector3)leftEye.GetColumn(2), leftEye.GetColumn(1));
        }
        Quaternion trackingToWorld = worldHeadRotation * Quaternion.Inverse(headRotation);
        pose = new Pose(worldHeadPosition - trackingToWorld * headPosition, trackingToWorld);
        return true;
    }
}
