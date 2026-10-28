namespace RobotLocomotion
{
    using Unity.XR.CoreUtils;
    using UnityEngine;

    // Stops the walking mode (stick mode) hand visuals from passing through level geometry.
    // Each visible hand chases its tracked controller with a sphere sweep copied from RobotRig,
    // so the controller keeps tracking freely while the hand stops at walls.
    // Runs after WalkingHeadCollision, so any head push this frame has already moved the controllers.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin))]
    public class WalkingHandCollision : MonoBehaviour
    {
        [Header("Hands")]
        [Tooltip("Tracked controller the left hand follows (Camera Offset > Left Controller).")]
        [SerializeField] private Transform leftController;
        [Tooltip("Tracked controller the right hand follows (Camera Offset > Right Controller).")]
        [SerializeField] private Transform rightController;
        [Tooltip("Visible left hand. Must not be a child of the left controller.")]
        [SerializeField] private Transform leftHand;
        [Tooltip("Visible right hand. Must not be a child of the right controller.")]
        [SerializeField] private Transform rightHand;
        [Tooltip("Centre of the left hand's collision sphere, in controller space.")]
        [SerializeField] private Vector3 leftHandOffset;
        [Tooltip("Centre of the right hand's collision sphere, in controller space.")]
        [SerializeField] private Vector3 rightHandOffset;
        [Tooltip("Keep in sync with LocomotionModeSwitcher's Hand Radius.")]
        [SerializeField, Min(0.001f)] private float handRadius = 0.05f;

        [Header("Collision")]
        [Tooltip("Keep in sync with RobotRig's Locomotion Enabled Layers. Exclude the layers the rig's own colliders are on.")]
        [SerializeField] private LayerMask locomotionEnabledLayers = ~Physics.IgnoreRaycastLayer;
        [SerializeField, Range(0.9f, 0.999f)] private float defaultPrecision = 0.995f;
        [Tooltip("How much of the blocked movement slides along the surface each frame. RobotRig uses 0.03.")]
        [SerializeField, Range(0f, 1f)] private float defaultSlideFactor = 0.25f;
        [Tooltip("A stuck hand snaps back to its controller once this far behind, if the head can see the controller.")]
        [SerializeField, Min(0.05f)] private float unStickDistance = 0.3f;
        [Tooltip("Controller jumps bigger than this in one frame (teleport, scene load) re-place the hand instead of sweeping.")]
        [SerializeField, Min(0.1f)] private float teleportThreshold = 0.75f;

        [Header("Mode")]
        [Tooltip("Optional. Crawl mode already handles hand collision through RobotRig.")]
        [SerializeField] private LocomotionModeSwitcher modeSwitcher;

        private Transform head;
        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private bool leftHandPlaced;
        private bool rightHandPlaced;

        private void Awake()
        {
            head = GetComponent<XROrigin>().Camera.transform;

            if (!ValidateReferences())
                enabled = false;
        }

        private void OnEnable()
        {
            leftHandPlaced = false;
            rightHandPlaced = false;
        }

        private bool ValidateReferences()
        {
            if (leftController == null || rightController == null || leftHand == null || rightHand == null)
            {
                Debug.LogError($"{nameof(WalkingHandCollision)}: controllers or hands are not assigned.", this);
                return false;
            }

            if (leftHand.IsChildOf(leftController) || rightHand.IsChildOf(rightController))
            {
                Debug.LogError($"{nameof(WalkingHandCollision)}: a hand is a child of its controller, " +
                               "so it would still follow the controller through walls.", this);
                return false;
            }

            return true;
        }

        private void LateUpdate()
        {
            if (modeSwitcher != null &&
                (modeSwitcher.CurrentMode == LocomotionModeSwitcher.Mode.Crawl || modeSwitcher.IsTransitioning))
            {
                leftHandPlaced = false;
                rightHandPlaced = false;
                return;
            }

            UpdateHand(leftController, leftHand, leftHandOffset, ref lastLeftHandPosition, ref leftHandPlaced);
            UpdateHand(rightController, rightHand, rightHandOffset, ref lastRightHandPosition, ref rightHandPlaced);
        }

        private void UpdateHand(Transform controller, Transform hand, Vector3 offset,
            ref Vector3 lastHandPosition, ref bool placed)
        {
            Vector3 targetPosition = PositionWithOffset(controller, offset);
            Vector3 movement = targetPosition - lastHandPosition;
            Vector3 handPosition;

            if (!placed || movement.sqrMagnitude > teleportThreshold * teleportThreshold)
            {
                handPosition = PlaceFromHead(targetPosition);
                placed = true;
            }
            else if (movement.sqrMagnitude < 1e-10f)
            {
                handPosition = lastHandPosition;
            }
            else if (IterativeCollisionSphereCast(lastHandPosition, handRadius, movement,
                         defaultPrecision, out Vector3 finalPosition, out _, false))
            {
                handPosition = ShouldUnstick(finalPosition, targetPosition)
                    ? PlaceFromHead(targetPosition)
                    : finalPosition;
            }
            else
            {
                handPosition = targetPosition;
            }

            lastHandPosition = handPosition;
            hand.SetPositionAndRotation(handPosition - controller.rotation * offset, controller.rotation);
        }

        // Starts a hand at its target, but never on the far side of a surface between head and hand.
        private Vector3 PlaceFromHead(Vector3 targetPosition)
        {
            Vector3 origin = head.position;
            Vector3 toTarget = targetPosition - origin;

            if (toTarget.sqrMagnitude > Mathf.Epsilon &&
                Physics.SphereCast(origin, handRadius, toTarget.normalized, out RaycastHit hit, toTarget.magnitude,
                    locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
            {
                return origin + toTarget.normalized * hit.distance;
            }

            return targetPosition;
        }

        private bool ShouldUnstick(Vector3 stuckPosition, Vector3 targetPosition)
        {
            Vector3 headPosition = head.position;
            Vector3 toTarget = targetPosition - headPosition;

            if ((targetPosition - stuckPosition).sqrMagnitude <= unStickDistance * unStickDistance)
            {
                return false;
            }

            return !Physics.SphereCast(headPosition, handRadius * defaultPrecision, toTarget,
                out _, toTarget.magnitude - handRadius,
                locomotionEnabledLayers, QueryTriggerInteraction.Ignore);
        }

        private static Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private bool IterativeCollisionSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector,
            float precision, out Vector3 endPosition, out Vector3 surfaceNormal, bool singleHand)
        {
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision,
                    out endPosition, out RaycastHit hitInfo))
            {
                surfaceNormal = hitInfo.normal;

                Vector3 firstPosition = endPosition;
                float slipPercentage = singleHand ? 0.001f : defaultSlideFactor;
                Vector3 projectedMovement = Vector3.ProjectOnPlane(
                    startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;

                if (CollisionsSphereCast(endPosition, sphereRadius, projectedMovement, precision * precision,
                        out endPosition, out hitInfo))
                {
                    surfaceNormal = hitInfo.normal;
                    return true;
                }

                Vector3 slidStart = projectedMovement + firstPosition;

                if (CollisionsSphereCast(slidStart, sphereRadius, startPosition + movementVector - slidStart,
                        precision * precision * precision, out endPosition, out hitInfo))
                {
                    surfaceNormal = hitInfo.normal;
                    return true;
                }

                endPosition = firstPosition;
                return true;
            }

            if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f,
                    movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f),
                    precision * 0.66f, out endPosition, out hitInfo))
            {
                surfaceNormal = hitInfo.normal;
                endPosition = startPosition;
                return true;
            }

            endPosition = Vector3.zero;
            surfaceNormal = Vector3.up;
            return false;
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector,
            float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo,
                    movementVector.magnitude + sphereRadius * (1f - precision),
                    locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
            {
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                if (Physics.SphereCast(startPosition, sphereRadius * precision * precision,
                        finalPosition - startPosition, out RaycastHit innerHit,
                        (finalPosition - startPosition).magnitude + sphereRadius * (1f - precision * precision),
                        locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized *
                        Mathf.Max(0f, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit,
                             (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f,
                             locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                }

                return true;
            }

            if (Physics.Raycast(startPosition, movementVector, out hitInfo,
                    movementVector.magnitude + sphereRadius * precision * 0.999f,
                    locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
            {
                finalPosition = startPosition;
                return true;
            }

            finalPosition = Vector3.zero;
            return false;
        }
    }
}
