using UnityEngine;

public class DoorBlock : MonoBehaviour, InteractableInterface
{
    public Door door;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact()
    {
        if (!door.isButtonDoor)
        {
            door.Interact();
        }
    }
}
