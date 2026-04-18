using UnityEngine;

public class ButtonTriggerpublic  : MonoBehaviour, InteractableInterface
{
    public ButtonInteract button;

    public void Interact()
    {
        Debug.Log("entered button trigger interact");
        if (button.playerPressable)
        {
            button.Interact();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("OnTriggerEnter");
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            button.pressed = true;
            button.Press();
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Debug.Log("OnTriggerExit");
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            button.Release();
            button.pressed = false;
        }
    }

    public void SetGlow(bool state)
    {
        // Nothing for now, maybe add an effect later
    }
}
