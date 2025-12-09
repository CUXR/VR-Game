using UnityEngine;
using System.Collections.Generic;

public class ButtonInteract : MonoBehaviour, InteractableInterface
{
    [SerializeField] public List<GameObject> interactableObject;
    public void Interact()
    {
        if (interactableObject.Count!=0)
        {
            foreach (GameObject obj in interactableObject) {
                InteractableInterface interactable = obj.GetComponent<InteractableInterface>();
                interactable.Interact();
            }
        }
    }
}
