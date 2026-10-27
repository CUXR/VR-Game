using UnityEngine;

public class Movable : MonoBehaviour, InteractableInterface
{
    // Seconds the object stays pinned after the VR player gets off it, so it doesn't flicker
    // between pinned and free while crawling across it.
    private const float PinReleaseDelay = 0.25f;

    private float radiusToDraw;
    private float soundTimer = 0f;
    private float soundRadiusDuration = 2f;

    [Header("Sound Variables")]
    private float objectVolumeRadius = 7f;
    private float objectVolumeDecay = 0.4f;
    private float objectLoudness = 0.3f;
    public float pushForce;
    private Rigidbody rb;
    private Transform currentInteractor;
    public Outline outline; // outline settings
    public string tutorialText = ""; // tutorial text, empty if nothing
    public AudioSource sound;

    private bool startedKinematic;
    private CollisionDetectionMode freeCollisionMode;
    private float lastStoodOnTime = float.NegativeInfinity;

    // While the VR player is standing or crawling on this object it is held in place,
    // otherwise the rig's movement shoves it out from under them.
    public bool IsPinned { get; private set; }

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        if (rb != null)
            startedKinematic = rb.isKinematic;
    }

    void FixedUpdate()
    {
        if (rb == null || startedKinematic)
            return;

        RobotLocomotion.RobotRig rig = RobotLocomotion.RobotRig.Instance;
        if (rig != null && rig.isActiveAndEnabled && rig.IsStandingOn(rb))
            lastStoodOnTime = Time.time;

        bool pin = Time.time - lastStoodOnTime < PinReleaseDelay;
        if (pin == IsPinned)
            return;

        IsPinned = pin;
        if (pin)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // Kinematic bodies only support speculative continuous collision.
            freeCollisionMode = rb.collisionDetectionMode;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = freeCollisionMode;
        }
    }

    public void SetInteractor(Transform interactor)
    {
        currentInteractor = interactor;
    }

    public void Interact()
    {
        if (currentInteractor == null) return;
        Debug.Log("Push");
        if (sound != null) sound.Play();
        Vector3 pushDir = (transform.position - currentInteractor.position).normalized;
        Push(pushDir, 1f);
    }

    // strength scales pushForce, so hand pushes can be softer than the full Interact() push.
    public void Push(Vector3 direction, float strength)
    {
        if (IsPinned) return;
        rb.AddForce(direction.normalized * pushForce * strength * transform.localScale.magnitude, ForceMode.Impulse);
    }

    public void SetGlow(bool state)
    {
        outline.enabled = state;
    }

    public string GetTutorialText()
    {
        return tutorialText;
    }

}
