namespace RobotLocomotion
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.XR.Interaction.Toolkit.Interactors;

    // VR counterpart of ObjectInteraction's highlight: each hand casts forward from its controller,
    // and whatever interactable it points at within range gets SetGlow(true).
    // An object stays lit while either hand points at it. A hand that is holding something doesn't highlight.
    // Casts from the tracked controllers, so it works the same in stick and crawl mode.
    [DisallowMultipleComponent]
    public class VRHandHighlighter : MonoBehaviour
    {
        [Header("Hands")]
        [Tooltip("Camera Offset > Left Controller.")]
        [SerializeField] private Transform leftController;
        [Tooltip("Camera Offset > Right Controller.")]
        [SerializeField] private Transform rightController;
        [Tooltip("Optional. While this interactor holds something, the left hand doesn't highlight.")]
        [SerializeField] private XRBaseInteractor leftGrabInteractor;
        [Tooltip("Optional. While this interactor holds something, the right hand doesn't highlight.")]
        [SerializeField] private XRBaseInteractor rightGrabInteractor;

        [Header("Cast")]
        [SerializeField, Min(0.05f)] private float range = 1f;
        [Tooltip("Radius of the sphere cast, so the hand doesn't need to point exactly at the object.")]
        [SerializeField, Min(0f)] private float castRadius = 0.05f;
        [Tooltip("Exclude the layers the rig's own colliders are on (Ignore Raycast).")]
        [SerializeField] private LayerMask highlightLayers = ~(1 << 2);

        private InteractableInterface leftTarget;
        private InteractableInterface rightTarget;

        // How many hands are pointing at each lit object.
        private readonly Dictionary<InteractableInterface, int> pointCounts = new Dictionary<InteractableInterface, int>();

        private void Awake()
        {
            if (leftController == null || rightController == null)
            {
                Debug.LogError($"{nameof(VRHandHighlighter)}: controllers are not assigned.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            SetTarget(ref leftTarget, null);
            SetTarget(ref rightTarget, null);
        }

        private void Update()
        {
            SetTarget(ref leftTarget, FindTarget(leftController, leftGrabInteractor));
            SetTarget(ref rightTarget, FindTarget(rightController, rightGrabInteractor));
        }

        private InteractableInterface FindTarget(Transform controller, XRBaseInteractor grabInteractor)
        {
            if (!controller.gameObject.activeInHierarchy)
                return null;

            if (grabInteractor != null && grabInteractor.hasSelection)
                return null;

            // Stops at the first solid thing, so walls block highlighting.
            if (!Physics.SphereCast(controller.position, castRadius, controller.forward, out RaycastHit hit, range,
                    highlightLayers, QueryTriggerInteraction.Ignore))
                return null;

            // Colliders often sit on child objects (the push cart's do), so search up the hierarchy.
            return hit.collider.GetComponentInParent<InteractableInterface>();
        }

        private void SetTarget(ref InteractableInterface current, InteractableInterface next)
        {
            if (ReferenceEquals(current, next))
                return;

            if (current != null)
                Release(current);
            if (next != null)
                Point(next);

            current = next;
        }

        private void Point(InteractableInterface target)
        {
            pointCounts.TryGetValue(target, out int count);
            pointCounts[target] = count + 1;
            if (count == 0)
                target.SetGlow(true);
        }

        private void Release(InteractableInterface target)
        {
            if (!pointCounts.TryGetValue(target, out int count))
                return;

            if (count > 1)
            {
                pointCounts[target] = count - 1;
                return;
            }

            pointCounts.Remove(target);

            // The object may have been destroyed while lit (e.g. a picked-up collectible).
            if (target is Object unityObject && unityObject == null)
                return;
            target.SetGlow(false);
        }
    }
}
