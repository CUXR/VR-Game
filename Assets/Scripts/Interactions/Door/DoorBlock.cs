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

    public void SetGlow(bool state)
    {
        // Nothing for now, maybe add an effect later
    }
}
