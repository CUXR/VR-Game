using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Lets a VR hand physically touch the world: press buttons, push Movables, and open unlocked doors.
// Also stops whatever the hand is holding from colliding with the player's own body.
// Lives on its own child of the controller, so its kinematic Rigidbody isn't merged into the XR Origin's.
[DisallowMultipleComponent]
public class HandContact : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float radius = 0.06f;
    [Tooltip("Interactor this hand grabs with. Objects it holds won't collide with the player's body.")]
    [SerializeField] private XRBaseInteractor grabInteractor;
    [SerializeField] private HapticImpulsePlayer haptics;
    [Tooltip("Seconds before the same Movable can be pushed again.")]
    [SerializeField, Min(0f)] private float pushCooldown = 0.5f;
    [Tooltip("Fraction of the Movable's pushForce applied by a hand moving at fullPushHandSpeed.")]
    [SerializeField, Range(0f, 1f)] private float pushStrength = 0.3f;
    [Tooltip("Hand speed (m/s) that gives the full pushStrength. Slower hands push proportionally softer.")]
    [SerializeField, Min(0.01f)] private float fullPushHandSpeed = 1.5f;

    private readonly Dictionary<Movable, float> lastPushTime = new Dictionary<Movable, float>();
    private readonly List<Collider> playerColliders = new List<Collider>();
    private Vector3 lastPosition;
    private Vector3 handVelocity;

    private void Awake()
    {
        if (!TryGetComponent(out Rigidbody rb))
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (!TryGetComponent(out SphereCollider sphere))
            sphere = gameObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = radius;

        if (haptics == null)
            haptics = GetComponentInParent<HapticImpulsePlayer>();

        XROrigin origin = GetComponentInParent<XROrigin>();
        if (origin != null)
        {
            foreach (Collider body in origin.GetComponentsInChildren<Collider>(true))
            {
                if (!body.isTrigger)
                    playerColliders.Add(body);
            }
        }
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
        handVelocity = Vector3.zero;

        if (grabInteractor == null)
            return;
        grabInteractor.selectEntered.AddListener(OnGrab);
        grabInteractor.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        if (grabInteractor == null)
            return;
        grabInteractor.selectEntered.RemoveListener(OnGrab);
        grabInteractor.selectExited.RemoveListener(OnRelease);
    }

    private void Update()
    {
        if (Time.deltaTime > 0f)
            handVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.GetComponentInParent<InteractableInterface>())
        {
            case ButtonTrigger:
                // The button presses itself from its own trigger; just give feedback.
                Pulse(0.5f, 0.05f);
                break;
            case Movable movable:
                Push(movable, other);
                break;
            case DoorBlock block:
                // DoorBlock only opens doors that aren't locked behind a button.
                block.Interact();
                break;
            case Door door when !door.isButtonDoor:
                door.Interact();
                break;
        }
    }

    private void Push(Movable movable, Collider touched)
    {
        if (movable.IsPinned)
            return;

        // A hand landing on top is climbing onto it, not pushing it.
        if (transform.position.y > touched.bounds.max.y - radius)
            return;

        if (lastPushTime.TryGetValue(movable, out float last) && Time.time - last < pushCooldown)
            return;

        // Push the way the hand is moving, never back toward the hand.
        Vector3 away = movable.transform.position - transform.position;
        Vector3 direction = Vector3.Dot(handVelocity, away) > 0f ? handVelocity : away;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float speedFactor = Mathf.Clamp01(handVelocity.magnitude / fullPushHandSpeed);
        if (speedFactor <= 0f)
            return;

        lastPushTime[movable] = Time.time;
        if (movable.sound != null)
            movable.sound.Play();
        movable.Push(direction, pushStrength * speedFactor);
        Pulse(0.4f, 0.08f);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        SetIgnoreBody(args.interactableObject, true);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // The other hand may still be holding it.
        if (!args.interactableObject.isSelected)
            SetIgnoreBody(args.interactableObject, false);
    }

    private void SetIgnoreBody(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable, bool ignore)
    {
        foreach (Collider held in interactable.colliders)
        {
            foreach (Collider body in playerColliders)
            {
                if (held != null && body != null)
                    Physics.IgnoreCollision(held, body, ignore);
            }
        }
    }

    private void Pulse(float amplitude, float duration)
    {
        if (haptics != null)
            haptics.SendHapticImpulse(amplitude, duration);
    }
}
