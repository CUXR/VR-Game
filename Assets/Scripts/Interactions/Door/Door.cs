using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, InteractableInterface
{
    public string keyName; //set keyName to "none" if the door doesn't have a key, otherwise match name of collectible object
    public Animator anim;
    [SerializeField]
    private bool inDoorPath = false;
    [SerializeField]
    public bool isButtonDoor = false;

    void Start()
    {
        //get the current position of the door
        door = gameObject.transform;
    }

    public void setOpenFalse()
    {
        anim.SetBool("open", false);
    }

    void Update()
    {
        //makes it so the door won't close on you
        anim.SetBool("inDoorPath", inDoorPath);

        if (inDoorPath)
        {
            anim.SetBool("open", true);
        }
       
    }

    public void Interact()
    {
        Debug.Log("entered door interact");
        //if no key needed for door, start opening
        if(keyName == "none")
        {
            //Debug.Log("entered key is null");
            anim.SetBool("open", true);
            return;
        }

        //Debug.Log("items: "+ PlayerBackpack.items[0] + PlayerBackpack.items[1] + PlayerBackpack.items[2]);
        //Debug.Log("result of IndexOf call in Door script: "+ Array.IndexOf(PlayerBackpack.items, keyName));
        //checks for required key in backpack
        if (Array.IndexOf(PlayerBackpack.items, keyName) != -1)
        {

            anim.SetBool("open", true);
        }
        else {
            Debug.Log("key required");
        }
    }

    //detects if player is in way of door closing
    public void OnTriggerStay(Collider collider)
    {
        Debug.Log("entered ontriggerstay");
        if (collider.CompareTag("Player") || collider.CompareTag("Holdable"))
        {
            //anim.SetBool("open", true);
            inDoorPath = true;
        }
    }

    //detects if player steps out of way of door closing
    public void OnTriggerExit(Collider collider)
    {
        Debug.Log("entered ontriggerexit");
        if (collider.CompareTag("Player") || collider.CompareTag("Holdable"))
        {
            anim.SetBool("open", false);
            inDoorPath = false;
        }
    }
    
}
