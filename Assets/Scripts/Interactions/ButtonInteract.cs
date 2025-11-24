using UnityEngine;

public class ButtonInteract : MonoBehaviour, InteractableInterface
{
    [SerializeField] public GameObject interactableObject;
    public void Interact()
    {
        if (interactableObject!=null)
        {
            InteractableInterface interactable = interactableObject.GetComponent<InteractableInterface>();
            interactable.Interact();
        }
    }
}
