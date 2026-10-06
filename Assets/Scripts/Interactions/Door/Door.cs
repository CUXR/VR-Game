using System;
//using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, InteractableInterface
{
    public Animator anim;
    [SerializeField]
    private bool inDoorPath = false;
    [SerializeField]
    public bool isButtonDoor = false; //when something is not a button door that is the same as being unlocked, when something is a button door with no button thats the same as being locked
    public string tutorialText = ""; // tutorial text, empty if nothing
    public GameObject[] buttonTriggers; //deactivate when door is locked, activate when door unlocked
    public GameObject[] lockVisuals; // what ever gameObjects need to change material when door is locked/unlocked
    public bool locked; //locked or unlocked (when you toggle this in inspector, the door material won't change)
    public Material lockedMat;
    public Material unlockedMat;

    public AudioSource sound;

    void Start()
    {
        //at start make sure door lock material is updated
        SetMat();
        SetTrigger();
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
        if (sound != null) sound.Play();
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

    public void SetMat()
    {
        if (locked)
        {
            for (int i = 0; i < lockVisuals.Length; i++)
            {
                lockVisuals[i].GetComponent<Renderer>().material = lockedMat;
            }
        }
        else
        {
            for (int i = 0; i < lockVisuals.Length; i++)
            {
                lockVisuals[i].GetComponent<Renderer>().material = unlockedMat;
            }
        }
    }

    public void SetTrigger()
    {
        if (locked)
        {
            for (int i = 0; i < buttonTriggers.Length; i++)
            {
                buttonTriggers[i].SetActive(false);
            }
        }
        else
        {
            for (int i = 0; i < buttonTriggers.Length; i++)
            {
                buttonTriggers[i].SetActive(true);
            }
        }
    }

    public void setOpenFalse()
    {
        anim.SetBool("open", false);
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
