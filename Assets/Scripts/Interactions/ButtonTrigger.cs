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
        Debug.Log("entered button trigger interact");
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
        button.transform.localPosition = endPos;
        pressed = true;
        yield return new WaitForSeconds(0.5f);
        button.transform.localPosition = startPos;
        pressed = false;
    }

    public void Press()
    {
        button.transform.localPosition = endPos;
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
        button.transform.localPosition = startPos;
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter");
        if (other.CompareTag("Player") || other.CompareTag("Holdable") || other.CompareTag("Wire"))
        {
            pressed = true;
            Press();
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log("OnTriggerExit");
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            Release();
            pressed = false;
        }
    }
}


