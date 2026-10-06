namespace RobotLocomotion
{
    using Unity.XR.CoreUtils;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.XR;
    using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
    using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

    // Toggles the XR Origin between XRI stick locomotion and RobotRig crawl locomotion.
    // Only one system drives the rig at a time:
    //   Stick: CharacterController + XRI providers, Rigidbody kinematic, RobotRig disabled.
    //   Crawl: RobotRig + dynamic Rigidbody, CharacterController and XRI locomotion disabled.
    // Runs before RobotRig so the lowered head position is in place when RobotRig reads it.
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin), typeof(RobotRig), typeof(Rigidbody))]
    public class LocomotionModeSwitcher : MonoBehaviour
    {
        public enum Mode
        {
            Stick,
            Crawl,
        }

        [Header("Stick Mode (XRI)")]
        [SerializeField] private GameObject xriLocomotion;
        [SerializeField] private GravityProvider gravityProvider;
        [SerializeField] private Behaviour[] controllerInputManagers;
        [Tooltip("Active only in stick mode (interactors, controller visuals).")]
        [SerializeField] private GameObject[] stickModeOnlyObjects;
        [Tooltip("Forced off when entering crawl mode. The controller input managers re-activate them on demand in stick mode.")]
        [SerializeField] private GameObject[] teleportInteractors;

        [Header("Crawl Mode (RobotRig)")]
        [SerializeField] private SphereCollider headCollider;
        [SerializeField] private CapsuleCollider bodyCollider;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHandFollower;
        [SerializeField] private Transform rightHandFollower;
        [Tooltip("Keep in sync with RobotRig's Locomotion Enabled Layers.")]
        [SerializeField] private LayerMask environmentLayers = ~0;
        [Tooltip("Keep in sync with RobotRig's Minimum Raycast Distance.")]
        [SerializeField, Min(0.001f)] private float handRadius = 0.05f;

        [Header("Transition")]
        [SerializeField, Min(0.1f)] private float crawlHeadHeight = 0.4f;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.5f;
        [SerializeField, Min(0.05f)] private float bodyRadius = 0.15f;

        [Header("Crawl Snap Turn")]
        [SerializeField] private float snapTurnAmount = 45f;
        [SerializeField, Range(0.1f, 1f)] private float turnPressThreshold = 0.75f;
        [SerializeField, Range(0f, 1f)] private float turnReleaseThreshold = 0.3f;

        [Header("Input")]
        [SerializeField] private InputActionProperty toggleInput = new InputActionProperty(
            new InputAction("Toggle Locomotion Mode", InputActionType.Button, "<XRController>{LeftHand}/{SecondaryButton}"));
        [SerializeField] private InputActionProperty crawlTurnInput = new InputActionProperty(
            new InputAction("Crawl Snap Turn", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}", expectedControlType: "Vector2"));

        [Header("Feedback")]
        [SerializeField] private HapticImpulsePlayer toggleHaptics;

        private XROrigin xrOrigin;
        private RobotRig robotRig;
        private Rigidbody rb;
        private CharacterController characterController;
        private Transform cameraOffset;
        private Transform head;

        private Mode mode = Mode.Stick;
        private bool transitioning;
        private float blend;
        private float crawlOffsetDelta;
        private float fallbackStandingOffset;
        private bool turnArmed = true;

        public Mode CurrentMode => mode;
        public bool IsTransitioning => transitioning;

        private void Awake()
        {
            xrOrigin = GetComponent<XROrigin>();
            robotRig = GetComponent<RobotRig>();
            rb = GetComponent<Rigidbody>();
            characterController = GetComponent<CharacterController>();
            cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;
            head = xrOrigin.Camera.transform;
            fallbackStandingOffset = cameraOffset.localPosition.y;
        }

        private void OnEnable()
        {
            EnableEmbeddedAction(toggleInput);
            EnableEmbeddedAction(crawlTurnInput);
        }

        private void OnDisable()
        {
            DisableEmbeddedAction(toggleInput);
            DisableEmbeddedAction(crawlTurnInput);
        }

        private void Start()
        {
            ConfigureBodyCollider();
            ApplyStickPhysics();
            if (characterController != null)
                characterController.enabled = true;
            SetStickSystemsActive(true);
        }

        private void Update()
        {
            if (transitioning)
            {
                StepTransition(Time.deltaTime);
            }
            else
            {
                if (toggleInput.action != null && toggleInput.action.WasPressedThisFrame())
                    Toggle();

                if (mode == Mode.Crawl && !transitioning)
                    HandleCrawlTurn();
            }

            if (mode == Mode.Crawl || transitioning)
                ApplyCameraOffset();

            if (mode == Mode.Crawl && !transitioning)
                FollowHeadWithBody();
        }

        public void Toggle()
        {
            if (transitioning)
                return;

            if (mode == Mode.Stick)
                BeginCrawl();
            else
                BeginStand();
        }

        private void BeginCrawl()
        {
            float headAboveOrigin = transform.InverseTransformPoint(head.position).y;
            float targetHeight = crawlHeadHeight / Mathf.Max(0.0001f, transform.lossyScale.y);
            crawlOffsetDelta = Mathf.Min(0f, targetHeight - headAboveOrigin);

            SetStickSystemsActive(false);
            if (characterController != null)
                characterController.enabled = false;

            // Rigidbody stays kinematic while the head lowers, so nothing moves the origin mid-transition.
            mode = Mode.Crawl;
            transitioning = true;
            Pulse(0.3f, 0.08f);
        }

        private void FinishCrawl()
        {
            SetCrawlCollidersEnabled(true);
            FollowHeadWithBody();
            PlaceFollower(leftHand, leftHandFollower);
            PlaceFollower(rightHand, rightHandFollower);
            SetFollowersVisible(true);

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            robotRig.InitializeValues();
            robotRig.enabled = true;
            turnArmed = false;
        }

        private void BeginStand()
        {
            float rise = -crawlOffsetDelta * transform.lossyScale.y;
            float radius = headCollider.radius * transform.lossyScale.y;
            if (rise > 0f && Physics.SphereCast(head.position, radius, transform.up, out _, rise,
                    environmentLayers, QueryTriggerInteraction.Ignore))
            {
                // Not enough headroom to stand here (e.g. inside a vent).
                Pulse(0.8f, 0.25f);
                return;
            }

            robotRig.enabled = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            ApplyStickPhysics();
            SetCrawlCollidersEnabled(false);
            SetFollowersVisible(false);

            mode = Mode.Stick;
            transitioning = true;
            Pulse(0.3f, 0.08f);
        }

        private void FinishStand()
        {
            ApplyCameraOffset();
            if (characterController != null)
                characterController.enabled = true;
            SetStickSystemsActive(true);
        }

        private void StepTransition(float deltaTime)
        {
            float target = mode == Mode.Crawl ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, deltaTime / transitionDuration);

            if (!Mathf.Approximately(blend, target))
                return;

            blend = target;
            transitioning = false;

            if (mode == Mode.Crawl)
                FinishCrawl();
            else
                FinishStand();
        }

        // XROrigin resets the Camera Offset height on tracking origin changes (recenter, boundary reset),
        // so while crawling or transitioning this is reapplied every frame instead of set once.
        private void ApplyCameraOffset()
        {
            float t = Mathf.SmoothStep(0f, 1f, blend);
            Vector3 position = cameraOffset.localPosition;
            position.y = StandingOffset() + crawlOffsetDelta * t;
            cameraOffset.localPosition = position;
        }

        // Mirrors XROrigin's own rule for the Camera Offset height.
        private float StandingOffset()
        {
            switch (xrOrigin.CurrentTrackingOriginMode)
            {
                case TrackingOriginModeFlags.Floor:
                    return 0f;
                case TrackingOriginModeFlags.Device:
                case TrackingOriginModeFlags.Unbounded:
                    return xrOrigin.CameraYOffset;
                default:
                    return fallbackStandingOffset;
            }
        }

        private void HandleCrawlTurn()
        {
            if (crawlTurnInput.action == null)
                return;

            float x = crawlTurnInput.action.ReadValue<Vector2>().x;

            if (Mathf.Abs(x) <= turnReleaseThreshold)
            {
                turnArmed = true;
                return;
            }

            if (!turnArmed || Mathf.Abs(x) < turnPressThreshold)
                return;

            turnArmed = false;
            float degrees = Mathf.Sign(x) * snapTurnAmount;
            robotRig.Turn(degrees);
            rb.linearVelocity = Quaternion.AngleAxis(degrees, transform.up) * rb.linearVelocity;

            // Followers rotated with the rig; resync RobotRig's cached hand/head positions to them.
            robotRig.InitializeValues();
        }

        // Keeps the body capsule under the head, with its base on the origin (the floor).
        private void FollowHeadWithBody()
        {
            Vector3 headLocal = transform.InverseTransformPoint(head.position);
            bodyCollider.transform.localPosition = new Vector3(headLocal.x, 0f, headLocal.z);
        }

        private void ConfigureBodyCollider()
        {
            float scale = Mathf.Max(0.0001f, transform.lossyScale.y);
            float height = crawlHeadHeight / scale;
            float radius = Mathf.Min(bodyRadius / scale, height * 0.5f);
            bodyCollider.direction = 1;
            bodyCollider.radius = radius;
            bodyCollider.height = height;
            bodyCollider.center = new Vector3(0f, height * 0.5f, 0f);
        }

        // Starts a follower at its hand, but never on the far side of a surface between head and hand.
        private void PlaceFollower(Transform hand, Transform follower)
        {
            Vector3 origin = head.position;
            Vector3 toHand = hand.position - origin;
            Vector3 target = hand.position;

            if (toHand.sqrMagnitude > Mathf.Epsilon &&
                Physics.SphereCast(origin, handRadius, toHand.normalized, out RaycastHit hit, toHand.magnitude,
                    environmentLayers, QueryTriggerInteraction.Ignore))
            {
                target = origin + toHand.normalized * hit.distance;
            }

            follower.position = target;
        }

        private void ApplyStickPhysics()
        {
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
        }

        private void SetStickSystemsActive(bool active)
        {
            if (!active)
            {
                foreach (GameObject teleport in teleportInteractors)
                {
                    if (teleport != null)
                        teleport.SetActive(false);
                }
            }

            foreach (GameObject stickObject in stickModeOnlyObjects)
            {
                if (stickObject != null)
                    stickObject.SetActive(active);
            }

            foreach (Behaviour manager in controllerInputManagers)
            {
                if (manager != null)
                    manager.enabled = active;
            }

            if (active && gravityProvider != null)
                gravityProvider.ResetFallForce();

            if (xriLocomotion != null)
                xriLocomotion.SetActive(active);
        }

        private void SetCrawlCollidersEnabled(bool enabled)
        {
            headCollider.enabled = enabled;
            bodyCollider.enabled = enabled;
        }

        private void SetFollowersVisible(bool visible)
        {
            leftHandFollower.gameObject.SetActive(visible);
            rightHandFollower.gameObject.SetActive(visible);
        }

        private void Pulse(float amplitude, float duration)
        {
            if (toggleHaptics != null)
                toggleHaptics.SendHapticImpulse(amplitude, duration);
        }

        private static void EnableEmbeddedAction(InputActionProperty property)
        {
            if (property.reference == null)
                property.action?.Enable();
        }

        private static void DisableEmbeddedAction(InputActionProperty property)
        {
            if (property.reference == null)
                property.action?.Disable();
        }
    }
}
