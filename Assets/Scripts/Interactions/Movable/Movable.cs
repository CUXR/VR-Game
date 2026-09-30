using UnityEngine;

public class Movable : MonoBehaviour, InteractableInterface
{
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
    public AudioSource sound_moving;
    public AudioSource sound_hit_wall;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    public void SetInteractor(Transform interactor)
    {
        currentInteractor = interactor;
    }

    void Update()
    {
        if (sound_moving != null && rb != null)
        {
            if (rb.linearVelocity.magnitude > 0.25f)
            {
                if (!sound_moving.isPlaying)
                {
                    sound_moving.Play();
                }
            }
            else
            {
                if (sound_moving.isPlaying)
                {
                    sound_moving.Pause(); 
                }
            }
        }
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
        outline.enabled = state;
    }

    public string GetTutorialText()
    {
        return tutorialText;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 2f) 
        {
            if (sound_hit_wall != null && !sound_hit_wall.isPlaying) 
            {
                sound_hit_wall.Play();
            }
        }
    }
}
