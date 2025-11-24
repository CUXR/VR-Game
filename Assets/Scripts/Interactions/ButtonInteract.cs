using UnityEngine;

public class ButtonInteract : MonoBehaviour, InteractableInterface
{
    [SerializeField] private InteractableInterface interactable;
    public void Interact()
    {
        interactable.Interact();
    }
}
