using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode crawlKey = KeyCode.C;
    public KeyCode altCrouchKey = KeyCode.LeftCommand;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode backpackKey = KeyCode.B;
    public KeyCode stabKey = KeyCode.Q;
    public KeyCode pauseKey = KeyCode.Escape;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
    }

    public Vector2 GetWalkDirection()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    public Vector2 GetLookDirection()
    {
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }

    public bool GetSprint()
    {
        return Input.GetKey(sprintKey);
    }

    public bool GetCrouchDown()
    {
        return Input.GetKeyDown(crouchKey) ^ Input.GetKeyDown(altCrouchKey);
    }

    public bool GetCrouchHold()
    {
        return Input.GetKey(crouchKey) ^ Input.GetKey(altCrouchKey);
    }

    public bool GetCrouchUp()
    {
        return Input.GetKeyUp(crouchKey) ^ Input.GetKeyUp(altCrouchKey);
    }
    public bool GetCrawlDown()
    {
        return Input.GetKeyDown(crawlKey);
    }

    public bool GetJumpDown()
    {
        return Input.GetKeyDown(jumpKey);
    }

    public bool GetInteractDown()
    {
        return Input.GetKeyDown(interactKey);
    }

    public bool GetBackpackDown()
    {
        return Input.GetKeyDown(backpackKey);
    }

    public bool GetStabDown()
    {
        return Input.GetKeyDown(stabKey);
    }
    
    public bool GetPauseDown()
    {
        return Input.GetKeyDown(pauseKey);
    }
}
