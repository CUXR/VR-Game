using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPosition;
    private TrackedPoseDriver trackedHead;
    private Vector3 initialHeadPose;
    private bool headPoseCalibrated;

    private void Awake()
    {
        trackedHead = GetComponentInChildren<TrackedPoseDriver>();
    }

    private void Update()
    {
        if (cameraPosition == null)
            return;

        if (trackedHead != null)
        {
            InputAction trackingAction = trackedHead.trackingStateInput.action;
            InputAction positionAction = trackedHead.positionInput.action;
            if (!headPoseCalibrated && trackingAction != null && positionAction != null
                && (trackingAction.ReadValue<int>() & 1) != 0)
            {
                initialHeadPose = positionAction.ReadValue<Vector3>();
                headPoseCalibrated = true;
            }
        }

        // Keep the original player spawn height while retaining real headset movement.
        transform.position = cameraPosition.position
            - transform.TransformVector(initialHeadPose);
    }
}
