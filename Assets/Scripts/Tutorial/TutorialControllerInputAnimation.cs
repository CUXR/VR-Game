using System;
using UnityEngine;
using UnityEngine.XR;

// Endpoints are sampled from the manufacturer's FBX, in its bone coordinates.
public sealed class TutorialControllerInputAnimation : MonoBehaviour
{
    public enum Control { X, Y, Menu, Trigger, Grip, StickUp, StickDown, StickLeft, StickRight }

    [Serializable]
    public struct BoneMotion
    {
        public Transform bone;
        public Control control;
        public Vector3 restPosition;
        public Quaternion restRotation;
        public Vector3 pressedPosition;
        public Quaternion pressedRotation;
    }

    [SerializeField] private BoneMotion[] motions;
    [SerializeField] private Transform glow;
    private Camera viewer;

    public void Configure(BoneMotion[] boneMotions, Transform gripGlow)
    {
        motions = boneMotions;
        glow = gripGlow;
    }

    private void LateUpdate()
    {
        if (motions == null) return;
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 stick);
        left.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick);
        foreach (var motion in motions)
        {
            if (motion.bone == null) continue;
            float value = Value(left, motion.control, stick);
            var position = Vector3.Lerp(motion.restPosition, motion.pressedPosition, value);
            var rotation = Quaternion.Slerp(motion.restRotation, motion.pressedRotation, value);
            if (motion.control >= Control.StickUp)
            {
                if (motion.control != Control.StickUp) continue;
                position = motion.restPosition;
                rotation = motion.restRotation;
                foreach (var axisMotion in motions)
                    if (axisMotion.bone == motion.bone && axisMotion.control >= Control.StickUp)
                        rotation *= Quaternion.Slerp(Quaternion.identity,
                            Quaternion.Inverse(axisMotion.restRotation) * axisMotion.pressedRotation,
                            Value(left, axisMotion.control, stick));
                if (stickClick)
                    position -= motion.bone.parent.InverseTransformVector(
                        motion.bone.TransformDirection(Vector3.up) * 0.001f);
            }
            motion.bone.SetLocalPositionAndRotation(position, rotation);
        }
        if (viewer == null) viewer = Camera.main;
        if (glow != null && viewer != null) glow.rotation = viewer.transform.rotation;
    }

    private static float Value(InputDevice device, Control control, Vector2 stick)
    {
        switch (control)
        {
            case Control.Trigger:
                return device.TryGetFeatureValue(CommonUsages.trigger, out float trigger) ? trigger : 0f;
            case Control.Grip:
                return device.TryGetFeatureValue(CommonUsages.grip, out float grip) ? grip : 0f;
            case Control.X: return Button(device, CommonUsages.primaryButton);
            case Control.Y: return Button(device, CommonUsages.secondaryButton);
            case Control.Menu: return Button(device, CommonUsages.menuButton);
            case Control.StickUp: return Mathf.Clamp01(stick.y);
            case Control.StickDown: return Mathf.Clamp01(-stick.y);
            case Control.StickLeft: return Mathf.Clamp01(-stick.x);
            case Control.StickRight: return Mathf.Clamp01(stick.x);
            default: return 0f;
        }
    }

    private static float Button(InputDevice device, InputFeatureUsage<bool> usage)
        => device.TryGetFeatureValue(usage, out bool pressed) && pressed ? 1f : 0f;
}
