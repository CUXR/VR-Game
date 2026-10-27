namespace RobotLocomotion
{
    using UnityEngine;
    using UnityEngine.XR.Interaction.Toolkit.Interactors;

    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class RobotRig : MonoBehaviour
    {
        public static RobotRig Instance { get; private set; }

        [Header("Colliders")]
        [SerializeField] private SphereCollider headCollider;
        [SerializeField] private CapsuleCollider bodyCollider;

        [Header("Hand Tracking")]
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private Transform leftHandFollower;
        [SerializeField] private Transform rightHandFollower;
        [SerializeField] private Vector3 leftHandOffset;
        [SerializeField] private Vector3 rightHandOffset;
        [Tooltip("While this hand's interactor is holding something, the hand doesn't crawl.")]
        [SerializeField] private XRBaseInteractor leftHandInteractor;
        [SerializeField] private XRBaseInteractor rightHandInteractor;

        [Header("Reach")]
        [SerializeField, Min(0.1f)] private float maxArmLength = 1.5f;
        [SerializeField, Min(0.1f)] private float unStickDistance = 1f;
        [SerializeField, Min(0.001f)] private float minimumRaycastDistance = 0.05f;
        [SerializeField, Range(0.9f, 0.999f)] private float defaultPrecision = 0.995f;
        [SerializeField, Range(0f, 1f)] private float defaultSlideFactor = 0.03f;

        [Header("Grip")]
        [SerializeField] private LayerMask locomotionEnabledLayers = ~0;
        [SerializeField, Range(0f, 89f)] private float maxGripAngle = 45f;

        [Header("Weight")]
        [SerializeField, Range(0.05f, 1f)] private float pullEfficiency = 0.75f;
        [SerializeField, Min(0.1f)] private float movementGain = 2.1f;
        [SerializeField, Range(0.05f, 1f)] private float climbEfficiency = 0.45f;
        [SerializeField, Min(0.1f)] private float maxCrawlSpeed = 4.5f;
        [SerializeField, Min(0f)] private float extraGravity = 12f;

        [Header("Friction")]
        [SerializeField, Min(0f)] private float kineticFriction = 0.35f;
        [SerializeField, Min(0f)] private float dragCoefficient = 0.55f;
        [SerializeField, Range(0f, 1f)] private float glideRetention = 0.8f;
        [Tooltip("Seconds over which the drag velocity is averaged for the release glide. 0 = last frame only.")]
        [SerializeField, Min(0f)] private float releaseVelocitySmoothing = 0.06f;
        [SerializeField, Min(0f)] private float groundFriction = 3f;
        [SerializeField, Min(0.01f)] private float groundCheckDistance = 0.3f;

        public bool disableMovement;

        private Rigidbody rb;
        private Vector3 lastLeftHandPosition;
        private Vector3 lastRightHandPosition;
        private Vector3 lastHeadPosition;
        private float gripNormalThreshold;
        private bool wasLeftHandTouching;
        private bool wasRightHandTouching;
        private bool wasLeftHandGripping;
        private bool wasRightHandGripping;
        private bool wasGripping;
        private Vector3 dragVelocity;
        private Vector3 glideVelocity;
        private Rigidbody groundBody;
        private Rigidbody leftGripBody;
        private Rigidbody rightGripBody;
        private readonly Collider[] overlapBuffer = new Collider[8];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            InitializeValues();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnValidate()
        {
            gripNormalThreshold = Mathf.Cos(maxGripAngle * Mathf.Deg2Rad);
        }

        private bool ValidateReferences()
        {
            if (headCollider == null || bodyCollider == null)
            {
                Debug.LogError($"{nameof(RobotRig)}: head or body collider is not assigned.", this);
                return false;
            }

            if (leftHandTransform == null || rightHandTransform == null ||
                leftHandFollower == null || rightHandFollower == null)
            {
                Debug.LogError($"{nameof(RobotRig)}: hand transforms or followers are not assigned.", this);
                return false;
            }

            if (locomotionEnabledLayers.value == 0)
            {
                Debug.LogError($"{nameof(RobotRig)}: Locomotion Enabled Layers is empty, movement will never register.", this);
                return false;
            }

            return true;
        }

        public void InitializeValues()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = true;
            // No interpolation: Update moves the rig through transform.position, and interpolation
            // would overwrite those moves with the interpolated physics pose between fixed steps.
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            gripNormalThreshold = Mathf.Cos(maxGripAngle * Mathf.Deg2Rad);

            lastLeftHandPosition = leftHandFollower.position;
            lastRightHandPosition = rightHandFollower.position;
            lastHeadPosition = headCollider.transform.position;

            wasLeftHandTouching = false;
            wasRightHandTouching = false;
            wasLeftHandGripping = false;
            wasRightHandGripping = false;
            wasGripping = false;
            dragVelocity = Vector3.zero;
            glideVelocity = Vector3.zero;
            groundBody = null;
            leftGripBody = null;
            rightGripBody = null;
        }

        private void FixedUpdate()
        {
            // Checked even while gripping so groundBody stays current for IsStandingOn.
            bool grounded = IsGrounded();

            if (wasLeftHandGripping || wasRightHandGripping)
            {
                return;
            }

            if (grounded)
            {
                Vector3 velocity = rb.linearVelocity;
                Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
                float speed = horizontal.magnitude;

                if (speed > Mathf.Epsilon)
                {
                    float decayed = Mathf.Max(0f, speed - groundFriction * Time.fixedDeltaTime);
                    horizontal *= decayed / speed;
                    rb.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
                }

                return;
            }

            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }

        private bool IsGrounded()
        {
            groundBody = null;
            Vector3 origin = bodyCollider.transform.TransformPoint(bodyCollider.center);

            if (!Physics.SphereCast(origin, bodyCollider.radius * 0.9f, Vector3.down, out RaycastHit hitInfo,
                    bodyCollider.height * 0.5f + groundCheckDistance,
                    locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (!IsGrippable(hitInfo.normal))
            {
                return false;
            }

            groundBody = hitInfo.rigidbody;
            return true;
        }

        // True while the player stands on, or crawls by gripping, the given rigidbody.
        public bool IsStandingOn(Rigidbody body)
        {
            return body != null && (body == groundBody || body == leftGripBody || body == rightGripBody);
        }

        private Rigidbody BodyUnderHand(Vector3 handPosition)
        {
            int count = Physics.OverlapSphereNonAlloc(handPosition, minimumRaycastDistance * 2f, overlapBuffer,
                locomotionEnabledLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Rigidbody body = overlapBuffer[i].attachedRigidbody;

                if (body != null && body != rb)
                {
                    return body;
                }
            }

            return null;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            bool leftHandTouching = false;
            bool rightHandTouching = false;
            bool leftHandGripping = false;
            bool rightHandGripping = false;

            Vector3 rigidBodyMovement = Vector3.zero;
            Vector3 firstIterationLeftHand = Vector3.zero;
            Vector3 firstIterationRightHand = Vector3.zero;

            bodyCollider.transform.eulerAngles = new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);

            // Runs before the hand and head casts below, so the glide gets the same head collision handling.
            ApplyGlide(deltaTime);

            bool leftHandFree = !IsHolding(leftHandInteractor);
            bool rightHandFree = !IsHolding(rightHandInteractor);

            Vector3 handGravityStep = Physics.gravity * (2f * deltaTime * deltaTime);

            Vector3 currentLeftHand = CurrentLeftHandPosition();
            Vector3 distanceTraveled = currentLeftHand - lastLeftHandPosition + handGravityStep;

            if (leftHandFree && IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out Vector3 finalPosition, out Vector3 surfaceNormal, true))
            {
                leftHandTouching = true;

                if (IsGrippable(surfaceNormal))
                {
                    leftHandGripping = true;
                    firstIterationLeftHand = (wasLeftHandGripping ? lastLeftHandPosition : finalPosition) - currentLeftHand;
                    rb.linearVelocity = Vector3.zero;
                }
            }

            Vector3 currentRightHand = CurrentRightHandPosition();
            distanceTraveled = currentRightHand - lastRightHandPosition + handGravityStep;

            if (rightHandFree && IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition, out surfaceNormal, true))
            {
                rightHandTouching = true;

                if (IsGrippable(surfaceNormal))
                {
                    rightHandGripping = true;
                    firstIterationRightHand = (wasRightHandGripping ? lastRightHandPosition : finalPosition) - currentRightHand;
                    rb.linearVelocity = Vector3.zero;
                }
            }

            bool twoHanded = (leftHandGripping || wasLeftHandGripping) && (rightHandGripping || wasRightHandGripping);

            rigidBodyMovement = twoHanded
                ? (firstIterationLeftHand + firstIterationRightHand) * 0.5f
                : firstIterationLeftHand + firstIterationRightHand;

            rigidBodyMovement = disableMovement
                ? Vector3.zero
                : ApplyWeight(rigidBodyMovement, deltaTime);

            if (IterativeCollisionSphereCast(lastHeadPosition, headCollider.radius,
                    headCollider.transform.position + rigidBodyMovement - lastHeadPosition,
                    defaultPrecision, out finalPosition, out _, false))
            {
                rigidBodyMovement = finalPosition - lastHeadPosition;

                Vector3 headSweep = headCollider.transform.position - lastHeadPosition + rigidBodyMovement;

                if (Physics.Raycast(lastHeadPosition, headSweep, out RaycastHit hitInfo,
                        headSweep.magnitude + headCollider.radius * defaultPrecision * 0.999f,
                        locomotionEnabledLayers, QueryTriggerInteraction.Ignore))
                {
                    rigidBodyMovement = lastHeadPosition - headCollider.transform.position;
                }
            }

            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position += rigidBodyMovement;
            }

            lastHeadPosition = headCollider.transform.position;

            currentLeftHand = CurrentLeftHandPosition();
            distanceTraveled = currentLeftHand - lastLeftHandPosition;

            if (leftHandFree && IterativeCollisionSphereCast(lastLeftHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition, out surfaceNormal, !twoHanded))
            {
                lastLeftHandPosition = finalPosition;
                leftHandTouching = true;
                leftHandGripping = IsGrippable(surfaceNormal);
            }
            else
            {
                lastLeftHandPosition = currentLeftHand;
            }

            currentRightHand = CurrentRightHandPosition();
            distanceTraveled = currentRightHand - lastRightHandPosition;

            if (rightHandFree && IterativeCollisionSphereCast(lastRightHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition, out surfaceNormal, !twoHanded))
            {
                lastRightHandPosition = finalPosition;
                rightHandTouching = true;
                rightHandGripping = IsGrippable(surfaceNormal);
            }
            else
            {
                lastRightHandPosition = currentRightHand;
            }

            if (leftHandTouching && ShouldUnstick(lastLeftHandPosition, CurrentLeftHandPosition()))
            {
                lastLeftHandPosition = CurrentLeftHandPosition();
                leftHandTouching = false;
                leftHandGripping = false;
            }

            if (rightHandTouching && ShouldUnstick(lastRightHandPosition, CurrentRightHandPosition()))
            {
                lastRightHandPosition = CurrentRightHandPosition();
                rightHandTouching = false;
                rightHandGripping = false;
            }

            leftHandFollower.position = lastLeftHandPosition;
            rightHandFollower.position = lastRightHandPosition;

            leftGripBody = leftHandGripping ? BodyUnderHand(lastLeftHandPosition) : null;
            rightGripBody = rightHandGripping ? BodyUnderHand(lastRightHandPosition) : null;

            bool grippingNow = leftHandGripping || rightHandGripping;

            if (grippingNow)
            {
                glideVelocity = Vector3.zero;
            }
            else if (wasGripping)
            {
                glideVelocity = new Vector3(dragVelocity.x, 0f, dragVelocity.z) * glideRetention;
            }

            wasGripping = grippingNow;

            wasLeftHandTouching = leftHandTouching;
            wasRightHandTouching = rightHandTouching;
            wasLeftHandGripping = leftHandGripping;
            wasRightHandGripping = rightHandGripping;
        }

        // The release glide moves the rig every rendered frame rather than through rb velocity,
        // which only advances at the fixed timestep and judders against the headset's refresh rate.
        private void ApplyGlide(float deltaTime)
        {
            if (glideVelocity == Vector3.zero || disableMovement)
            {
                return;
            }

            if (IsGrounded())
            {
                float speed = glideVelocity.magnitude;
                float decayed = speed - groundFriction * deltaTime;

                if (decayed <= 0.01f)
                {
                    glideVelocity = Vector3.zero;
                    return;
                }

                glideVelocity *= decayed / speed;
            }

            Vector3 step = glideVelocity * deltaTime;

            // Floors and ramps under the body are ignored; walls stop the glide and slide it along them.
            if (rb.SweepTest(step.normalized, out RaycastHit hit, step.magnitude, QueryTriggerInteraction.Ignore) &&
                !IsGrippable(hit.normal))
            {
                step = step.normalized * Mathf.Max(0f, hit.distance - minimumRaycastDistance * 0.1f);
                glideVelocity = Vector3.ProjectOnPlane(glideVelocity, hit.normal);
                glideVelocity.y = 0f;
            }

            transform.position += step;
        }

        private static bool IsHolding(XRBaseInteractor interactor)
        {
            return interactor != null && interactor.isActiveAndEnabled && interactor.hasSelection;
        }

        private Vector3 ApplyWeight(Vector3 movement, float deltaTime)
        {
            if (movement == Vector3.zero || deltaTime <= 0f)
            {
                if (deltaTime > 0f)
                {
                    SmoothDragVelocity(Vector3.zero, deltaTime);
                }

                return Vector3.zero;
            }

            Vector3 velocity = movement / deltaTime * pullEfficiency * movementGain;

            if (velocity.y > 0f)
            {
                velocity.y *= climbEfficiency;
            }

            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontal.magnitude;

            if (speed > Mathf.Epsilon)
            {
                float resisted = Mathf.Max(0f, speed - kineticFriction) / (1f + dragCoefficient * speed);
                horizontal *= resisted / speed;
            }

            velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

            if (velocity.sqrMagnitude > maxCrawlSpeed * maxCrawlSpeed)
            {
                velocity = velocity.normalized * maxCrawlSpeed;
            }

            SmoothDragVelocity(velocity, deltaTime);

            return velocity * deltaTime;
        }

        // Averages the per-frame drag velocity so a single noisy or repeated tracking frame
        // doesn't decide the release glide.
        private void SmoothDragVelocity(Vector3 target, float deltaTime)
        {
            float t = releaseVelocitySmoothing <= 0f ? 1f : 1f - Mathf.Exp(-deltaTime / releaseVelocitySmoothing);
            dragVelocity = Vector3.Lerp(dragVelocity, target, t);
        }

        private bool IsGrippable(Vector3 surfaceNormal)
        {
            return Vector3.Dot(surfaceNormal, Vector3.up) >= gripNormalThreshold;
        }

        private bool ShouldUnstick(Vector3 stuckPosition, Vector3 targetPosition)
        {
            Vector3 headPosition = headCollider.transform.position;
            Vector3 toTarget = targetPosition - headPosition;

            if ((targetPosition - stuckPosition).sqrMagnitude <= unStickDistance * unStickDistance)
            {
                return false;
            }

            return !Physics.SphereCast(headPosition, minimumRaycastDistance * defaultPrecision, toTarget,
                out _, toTarget.magnitude - minimumRaycastDistance,
                locomotionEnabledLayers, QueryTriggerInteraction.Ignore);
        }

        private Vector3 CurrentLeftHandPosition()
        {
            return ClampedHandPosition(PositionWithOffset(leftHandTransform, leftHandOffset));
        }

        private Vector3 CurrentRightHandPosition()
        {
            return ClampedHandPosition(PositionWithOffset(rightHandTransform, rightHandOffset));
        }

        private Vector3 ClampedHandPosition(Vector3 handPosition)
        {
            Vector3 headPosition = headCollider.transform.position;
            Vector3 toHand = handPosition - headPosition;

            return toHand.sqrMagnitude < maxArmLength * maxArmLength
                ? handPosition
                : headPosition + toHand.normalized * maxArmLength;
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

        public bool IsHandTouching(bool forLeftHand)
        {
            return forLeftHand ? wasLeftHandTouching : wasRightHandTouching;
        }

        public bool IsHandGripping(bool forLeftHand)
        {
            return forLeftHand ? wasLeftHandGripping : wasRightHandGripping;
        }

        public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
        }
    }
}