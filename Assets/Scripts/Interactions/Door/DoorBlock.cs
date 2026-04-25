using UnityEngine;

public class DoorBlock : MonoBehaviour, InteractableInterface
{
    public Door door;
    public string tutorialText = ""; // tutorial text, empty if nothing

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

    public string GetTutorialText()
    {
        return tutorialText;
    }
}
