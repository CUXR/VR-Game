using UnityEngine;
using UnityEngine.InputSystem;

public class TrackedControllerVisuals : MonoBehaviour
{
    [SerializeField] private GameObject leftControllerPrefab;
    [SerializeField] private GameObject rightControllerPrefab;
    private Transform leftAnchor;
    private Transform rightAnchor;
    private InputAction leftPosition;
    private InputAction leftRotation;
    private InputAction leftTrackingState;
    private InputAction rightPosition;
    private InputAction rightRotation;
    private InputAction rightTrackingState;

    private void Awake()
    {
        leftAnchor = CreateVisual("Left Controller Pose", leftControllerPrefab);
        rightAnchor = CreateVisual("Right Controller Pose", rightControllerPrefab);
    }

    private void LateUpdate()
    {
        UpdateVisuals();
    }

    private void OnEnable()
    {
        leftPosition = CreatePoseAction("Left Position", "<XRController>{LeftHand}/devicePosition", "Vector3");
        leftRotation = CreatePoseAction("Left Rotation", "<XRController>{LeftHand}/deviceRotation", "Quaternion");
        leftTrackingState = CreatePoseAction("Left Tracking", "<XRController>{LeftHand}/trackingState", "Integer");
        rightPosition = CreatePoseAction("Right Position", "<XRController>{RightHand}/devicePosition", "Vector3");
        rightRotation = CreatePoseAction("Right Rotation", "<XRController>{RightHand}/deviceRotation", "Quaternion");
        rightTrackingState = CreatePoseAction("Right Tracking", "<XRController>{RightHand}/trackingState", "Integer");
        Application.onBeforeRender += UpdateVisuals;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateVisuals;
        leftPosition?.Dispose();
        leftRotation?.Dispose();
        leftTrackingState?.Dispose();
        rightPosition?.Dispose();
        rightRotation?.Dispose();
        rightTrackingState?.Dispose();
    }

    private void UpdateVisuals()
    {
        UpdateVisual(leftAnchor, leftPosition, leftRotation, leftTrackingState);
        UpdateVisual(rightAnchor, rightPosition, rightRotation, rightTrackingState);
    }

    private Transform CreateVisual(string name, GameObject prefab)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.SetParent(transform, false);
        anchor.SetActive(false);

        if (prefab != null)
            Instantiate(prefab, anchor.transform, false);

        return anchor.transform;
    }

    private static InputAction CreatePoseAction(string name, string binding, string controlType)
    {
        InputAction action = new InputAction(name, InputActionType.PassThrough,
            binding, expectedControlType: controlType);
        action.Enable();
        return action;
    }

    private static void UpdateVisual(Transform anchor, InputAction position,
        InputAction rotation, InputAction trackingState)
    {
        if (anchor == null || anchor.childCount == 0)
            return;

        bool hasPose = position != null && rotation != null && trackingState != null
            && (trackingState.ReadValue<int>() & 3) == 3;
        if (!hasPose)
        {
            anchor.gameObject.SetActive(false);
            return;
        }

        anchor.localPosition = position.ReadValue<Vector3>();
        anchor.localRotation = rotation.ReadValue<Quaternion>();
        anchor.gameObject.SetActive(true);
    }
}
