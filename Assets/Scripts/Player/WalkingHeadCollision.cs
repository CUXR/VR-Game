namespace RobotLocomotion
{
    using Unity.XR.CoreUtils;
    using UnityEngine;

    // Stops the HMD from physically moving through level geometry while walking (stick mode)
    // by shifting the XR Origin back. The sweep is a copy of RobotRig's head collision check.
    // Runs in LateUpdate, after tracking and XRI locomotion have both moved the head this frame.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin))]
    public class WalkingHeadCollision : MonoBehaviour
    {
        [Header("Head")]
        [Tooltip("Optional. If set, its radius is used so walking and crawling share one head size.")]
        [SerializeField] private SphereCollider headCollider;
        [SerializeField, Min(0.01f)] private float headRadius = 0.12f;

        [Header("Collision")]
        [Tooltip("Keep in sync with RobotRig's Locomotion Enabled Layers. Exclude the player's own layer.")]
        [SerializeField] private LayerMask locomotionEnabledLayers = ~0;
        [SerializeField, Range(0.9f, 0.999f)] private float defaultPrecision = 0.995f;
        [SerializeField, Range(0f, 1f)] private float defaultSlideFactor = 0.03f;
        [Tooltip("Head jumps bigger than this in one frame (teleport, scene load) are not blocked.")]
        [SerializeField, Min(0.1f)] private float teleportThreshold = 0.75f;

        [Header("Mode")]
        [Tooltip("Optional. Crawl mode already handles head collision through RobotRig.")]
        [SerializeField] private LocomotionModeSwitcher modeSwitcher;

        private XROrigin xrOrigin;
        private Transform head;
        private Vector3 lastHeadPosition;
        private bool hasLastHeadPosition;

        private float HeadRadius => headCollider != null
            ? headCollider.radius * headCollider.transform.lossyScale.y
            : headRadius;

        private void Awake()
        {
            xrOrigin = GetComponent<XROrigin>();
            head = xrOrigin.Camera.transform;
        }

        private void OnEnable()
        {
            hasLastHeadPosition = false;
        }

        private void LateUpdate()
        {
            if (modeSwitcher != null &&
                (modeSwitcher.CurrentMode == LocomotionModeSwitcher.Mode.Crawl || modeSwitcher.IsTransitioning))
            {
                hasLastHeadPosition = false;
                return;
            }

            Vector3 headPosition = head.position;

            if (!hasLastHeadPosition ||
                (headPosition - lastHeadPosition).sqrMagnitude > teleportThreshold * teleportThreshold)
            {
                lastHeadPosition = headPosition;
                hasLastHeadPosition = true;
                return;
            }

            float radius = HeadRadius;
            Vector3 originMovement = Vector3.zero;

            if (IterativeCollisionSphereCast(lastHeadPosition, radius, headPosition - lastHeadPosition,
                    defaultPrecision, out Vector3 finalPosition, out _, false))
            {
                // RobotRig uses finalPosition - lastHeadPosition, which works there because hand pulls
                // dominate. Here the movement is all physical head motion, so correct from the current head.
                originMovement = finalPosition - headPosition;

                Vector3 headSweep = headPosition - lastHeadPosition + originMovement;

                if (Physics.Raycast(lastHeadPosition, headSweep, out RaycastHit hitInfo,
                        headSweep.magnitude + radius * defaultPrecision * 0.999f,
                        locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
                {
                    originMovement = lastHeadPosition - headPosition;
                }
            }

            // Only push sideways, so leaning over a table or under a shelf never lifts or drops the player.
            originMovement.y = 0f;

            if (originMovement != Vector3.zero)
            {
                transform.position += originMovement;
                // Keeps the CharacterController from snapping the origin back on its next Move.
                Physics.SyncTransforms();
            }

            lastHeadPosition = head.position;
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
