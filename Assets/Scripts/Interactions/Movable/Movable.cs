using UnityEngine;

public class Movable : MonoBehaviour, InteractableInterface
{
    private float radiusToDraw;
    private float soundTimer = 0f;
    private float soundRadiusDuration = 2f;

    [Header("Sound Variables")]
    private float objectVolumeRadius = 7f;
    private float objectVolumeDecay = 0.4f;
    private float objectLoudness = 1.0f;
    public float pushForce;
    private Rigidbody rb;
    private Transform currentInteractor;
    public Outline outline; // outline settings
    public string tutorialText = ""; // tutorial text, empty if nothing
    public AudioSource sound;

    private bool isTouchingSurface = false;

    [Header("SFX")]
    public float minVelocity = 0.1f; // Minimum speed for sound to play
    public float maxVelocity = 3.0f; // Speed at which volume reaches maximum
    public float fadeSpeed = 8.0f;   // How fast volume adjusts to speed changes

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();

        if (sound != null) 
        {
            sound.loop = true; 
            sound.volume = 0f;
            sound.Play();
        }
    }

    void Update()
    {
        if (sound == null || rb == null) return;

        float speed = rb.linearVelocity.magnitude;

        if (speed > minVelocity && isTouchingSurface)
        {
            // Map speed to volume level, capped at objectLoudness
            float speedRatio = Mathf.InverseLerp(minVelocity, maxVelocity, speed);
            float targetVolume = speedRatio * objectLoudness;
            
            sound.volume = Mathf.Lerp(sound.volume, targetVolume, Time.deltaTime * fadeSpeed);
        }
        else
        {
            // Fade out when slowing
            sound.volume = Mathf.Lerp(sound.volume, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        isTouchingSurface = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isTouchingSurface = false;
    }

    public void SetInteractor(Transform interactor)
    {
        currentInteractor = interactor;
    }

    public void Interact()
    {
        if (currentInteractor == null) return;
        Debug.Log("Push");
        Vector3 pushDir = (transform.position - currentInteractor.position).normalized;
        rb.AddForce(pushDir * pushForce * transform.localScale.magnitude, ForceMode.Impulse);
    }

    public void SetGlow(bool state)
    {
        if (outline != null)
        {
            outline.enabled = state;
        }
        else
        {
            Debug.LogWarning(gameObject.name + " is missing its Outline reference in the Inspector!");
        }
    }

    public string GetTutorialText()
    {
        return tutorialText;
    }

}
