using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;

public class ButtonInteract : MonoBehaviour, InteractableInterface
{
    //Stuff used to lerp button (gave up on lerp didn't work out)
    //public float moveAmount = 0.1f;
    //public float duration = 1f;
    //public float shift = 0.5f;
    //[SerializeField] private float timeElapsed = 0f;
    //[SerializeField] private bool pressing = false;
    //[SerializeField] private bool releasing = false;
    private Vector3 startPos;
    private Vector3 endPos;
    public GameObject pressedPos;

    public bool pressed = false;
    [SerializeField] public List<GameObject> interactableObject;

    void Start()
    {
        startPos = transform.localPosition;
        endPos = pressedPos.transform.localPosition;
    }

    //only for when player clicks
    public void Interact()
    {
        Debug.Log("button interacted with");
        if (interactableObject.Count!=0)
        {
            Debug.Log("there are interactable objects");
            foreach (GameObject obj in interactableObject) {
                InteractableInterface interactable = obj.GetComponent<InteractableInterface>();
                interactable.Interact();
            }
        }

        if (!pressed)
        {
            StartCoroutine(PressRoutine());
            pressed = false;
        }
    }

    IEnumerator PressRoutine() {
        transform.localPosition = endPos;
        pressed = true;
        yield return new WaitForSeconds(0.2f);
        transform.localPosition = startPos;
        pressed = false;
    }

    public void Press()
    {
        transform.localPosition = endPos;
        if (interactableObject.Count!=0)
        {
            Debug.Log("there are interactable objects");
            foreach (GameObject obj in interactableObject) {
                InteractableInterface interactable = obj.GetComponent<InteractableInterface>();
                interactable.Interact();
            }
        }

        // pressing = true;
        //transform.localPosition = startPos;
        // while (pressing)
        // {
        //     timeElapsed += Time.deltaTime;
        //     float t = timeElapsed / duration;

        //     transform.localPosition = Vector3.Lerp(startPos, endPos, t);

        //     if (t >= 1f) {
        //         pressing = false;
        //     }
        // }
        // timeElapsed = 0;
    }

    public void Release()
    {
        transform.localPosition = startPos;
    
        //releasing = true;
        //transform.localPosition = endPos;

        // while (releasing)
        // {
        //     timeElapsed += Time.deltaTime;
        //     float t = timeElapsed / duration;

        //     transform.localPosition = Vector3.Lerp(endPos, startPos, t);

        //     if (t >= 1f) {
        //         releasing = false;
        //     }
        // }
        // timeElapsed = 0;
    }
}
