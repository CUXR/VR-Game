using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;

public class ButtonInteract : MonoBehaviour, InteractableInterface
{
    private Vector3 startPos;
    private Vector3 endPos;
    public GameObject pressedPos;

    public bool playerPressable = true;
    public bool pressed = false;
    [SerializeField] public List<GameObject> interactableObject;
    public string tutorialText = ""; // tutorial text, empty if nothing

    void Start()
    {
        startPos = transform.localPosition;
        endPos = pressedPos.transform.localPosition;
    }

    //only for when player clicks
    public void Interact()
    {
        if (playerPressable)
        {
            if (interactableObject.Count!=0)
            {
                Press();
            }
            if (!pressed)
            {
                StartCoroutine(PressRoutine());
                pressed = false;
            }
        }
    }

    IEnumerator PressRoutine() {
        transform.localPosition = endPos;
        pressed = true;
        yield return new WaitForSeconds(0.5f);
        transform.localPosition = startPos;
        pressed = false;
    }

    public void Press()
    {
        transform.localPosition = endPos;
        if (interactableObject.Count!=0)
        {
            foreach (GameObject obj in interactableObject) {
                InteractableInterface interactable = obj.GetComponent<InteractableInterface>();
                interactable.Interact();
            }
        }
    }

    public void Release()
    {
        transform.localPosition = startPos;
    }

     void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            pressed = true;
            Press();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            pressed = false;
            Release();
        }
    }

    public void SetGlow(bool state)
    {
        // Nothing for now, maybe add an effect later
    }

    public string GetTutorialText()
    {
        return tutorialText;
    }
}
