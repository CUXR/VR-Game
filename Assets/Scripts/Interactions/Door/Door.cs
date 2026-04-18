using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, InteractableInterface
{
    public Animator anim;
    [SerializeField]
    private bool inDoorPath = false;
    [SerializeField]
    public bool isButtonDoor = false;

    public void setOpenFalse()
    {
        anim.SetBool("open", false);
    }

    void Update()
    {
        // Makes it so the door won't close on you
        anim.SetBool("inDoorPath", inDoorPath);

        if (inDoorPath)
        {
            anim.SetBool("open", true);
        }
       
    }

    public void Interact()
    {
        anim.SetBool("open", true);
    }

    // Detects if player is in way of door closing
    public void OnTriggerStay(Collider collider)
    {;
        if (collider.CompareTag("Player") || collider.CompareTag("Holdable"))
        {
            inDoorPath = true;
        }
    }

    // Detects if player steps out of way of door closing
    public void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("Player") || collider.CompareTag("Holdable"))
        {
            anim.SetBool("open", false);
            inDoorPath = false;
        }
    }

    public void SetGlow(bool state)
    {
        // Nothing for now, maybe add an effect later
    }
    
}
