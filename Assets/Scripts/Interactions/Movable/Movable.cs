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

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
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
        outline.enabled = state;
    }

    public string GetTutorialText()
    {
        return tutorialText;
    }

}
