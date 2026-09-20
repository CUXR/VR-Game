using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ButtonTrigger  : MonoBehaviour, InteractableInterface
{
    private Vector3 startPos;
    private Vector3 endPos;
    public GameObject pressedPos;

    public bool playerPressable = true;
    public bool pressed = false;
    [SerializeField] public List<GameObject> interactableObject;
    public GameObject button;
    public string tutorialText = ""; // tutorial text, empty if nothing
    public AudioSource sound;
    public float pressDuration = 0.5f; // how long a player press stays down

    // Colliders currently touching the button. While this is non-empty the
    // button is held down; it only springs back once the last one leaves.
    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private Coroutine playerPressRoutine;

    void Start()
    {
        startPos = button.transform.localPosition;
        endPos = pressedPos.transform.localPosition;
    }

    public void SetGlow(bool state)
    {
        // Nothing for now, maybe add an effect later
    }

    public string GetTutorialText()
    {
        return tutorialText;
    }

    public void Interact()
    {
        if (playerPressable)
        {
            if (playerPressRoutine != null) StopCoroutine(playerPressRoutine);
            playerPressRoutine = StartCoroutine(PressRoutine());
        }
    }

    // A player press is momentary: down, then back up after pressDuration
    // unless something is resting on the button by then.
    IEnumerator PressRoutine() {
        Press();
        yield return new WaitForSeconds(pressDuration);
        playerPressRoutine = null;
        if (occupants.Count == 0) Release();
    }

    public void Press()
    {
        if (sound != null) sound.Play();
        button.transform.localPosition = endPos;
        pressed = true;
        TriggerTargets();
    }

    public void Release()
    {
        button.transform.localPosition = startPos;
        pressed = false;
    }

    private void TriggerTargets()
    {
        foreach (GameObject obj in interactableObject)
        {
            if (obj == null) continue;
            InteractableInterface interactable = obj.GetComponent<InteractableInterface>();
            if (interactable != null) interactable.Interact();
        }
    }

    private bool CanPress(Collider other)
    {
        return other.CompareTag("Player") || other.CompareTag("Holdable") || other.CompareTag("Wire");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!CanPress(other)) return;
        bool wasEmpty = occupants.Count == 0;
        if (!occupants.Add(other)) return;
        if (wasEmpty) Press();
    }

    void OnTriggerExit(Collider other)
    {
        if (!occupants.Remove(other)) return;
        ReleaseIfEmpty();
    }

    private void ReleaseIfEmpty()
    {
        // While a player press is still running, let it do the release.
        if (occupants.Count == 0 && playerPressRoutine == null) Release();
    }
}
