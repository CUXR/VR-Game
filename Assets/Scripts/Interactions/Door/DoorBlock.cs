using UnityEngine;

public class DoorBlock : MonoBehaviour, InteractableInterface
{
    public Door door;

    public void Interact()
    {
        if (!door.isButtonDoor)
        {
            door.Interact();
        }
    }
}
