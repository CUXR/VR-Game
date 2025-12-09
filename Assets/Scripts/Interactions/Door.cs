using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, Interactable
{
    public String keyName; //set keyName to "none" if the door doesn't have a key, otherwise match name of collectible object
    private Transform door;
    //public SoundPlayer openEffect;
    public Vector3 closedPos;
    public Vector3 openPos;
    public float openingTime;
    public float holdOpen;
    private float openingComplete;
    [SerializeField] 
    private bool opening = false;
    [SerializeField] 
    private bool closing = false;
    //[SerializeField]
    private float timer;
    [SerializeField]
    private bool inDoorPath = false;

    //TODO: modify for button/buttons/keys?
    //TODO:

    void Start()
    {
        //get the current position of the door
        door = gameObject.transform;
        //initialize timer
        timer = 0f;
    }

    void Update()
    {
        //makes it so the door won't close on you
        if (closing && inDoorPath)
        {
            return;
        }

        //when door finishes opening, record time taken to open, end opening state, and set the timer
        if (opening && door.position == openPos)
        {
            openingComplete = Time.time;
            opening = false;
            timer = 1f;
        }
        //when door finishes closing, the closing state ends and time set back to initial (ready to start over)
        else if (closing && door.position == closedPos)
        {
            opening = false;
            closing = false;
            timer = 0f;
        }
        //if in process of opening or closing, move the door accordingly
        else if (opening || closing)
        {
            if(opening) {timer += Time.deltaTime;}
            if(closing) {timer -= Time.deltaTime;}

            timer = Mathf.Clamp(timer, 0f, openingTime);
            float t = timer / openingTime;
            // Move door
            door.localPosition = Vector3.Lerp(closedPos, openPos, t);
        }

        //checks when door has been open for holdOpen time and switches to closing state
        if (openingComplete > 0 && Time.time > openingComplete + holdOpen)
        {
            openingComplete = -1f;
            opening = false;
            closing = true;
        }
    }

    //"applied only if the player is not currently holding an object"
    //should this apply to a door tho?
    //"Takes in a collider (player collider) as well as three floats that determine the offset in the player's camera view."
    //what does this mean?
    //
    //how door reacts in different states and key collection progress to player interaction (press E)
    public void InteractWith()
    {
        //no reaction if door is in process of opening/closing
        if(opening || closing)
        {
            return;
        }
        //Debug.Log("entered interact with");
        //if no key needed for door, start opening
        if(keyName == "none")
        {
            //Debug.Log("entered key is null");
            closing = false;
            opening = true;
            return;
        }

        //Debug.Log("items: "+ PlayerBackpack.items[0] + PlayerBackpack.items[1] + PlayerBackpack.items[2]);
        //Debug.Log("result of IndexOf call in Door script: "+ Array.IndexOf(PlayerBackpack.items, keyName));
        //checks for required key in backpack
        if (Array.IndexOf(PlayerBackpack.items, keyName) != -1)
        {
            closing = false;
            opening = true;
        }
        else {
            Debug.Log("key required");
        }
    }

    //shouldn't this be more specifically for holdable?
    public void Release()
    {
        //doesn't apply to door
    }

    //where is this used?
    public bool IsHoldable()
    {
        return false;
    }

    //detects if player is in way of door closing
    public void OnTriggerEnter(Collider collider)
    {
        //Debug.Log("entered ontriggerenter");
        if (collider.CompareTag("Player") || collider.CompareTag("Holdable"))
        {
            inDoorPath = true;
        }
    }

    //detects if player steps out of way of door closing
    public void OnTriggerExit(Collider collider)
    {
        //Debug.Log("entered ontriggerexit");
        if (collider.CompareTag("Player") || collider.CompareTag("Holdable"))
        {
            inDoorPath = false;
        }
    }
    
}
