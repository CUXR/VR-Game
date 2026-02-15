using UnityEngine;

public class ButtonTriggerpublic  : MonoBehaviour
{
    public ButtonInteract button;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter");
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            button.pressed = true;
            button.Press();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit");
        if (other.CompareTag("Player") || other.CompareTag("Holdable"))
        {
            button.Release();
            button.pressed = false;
        }
    }
}
