using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{
    [SerializeField]
    private KeyCode walkForward = KeyCode.W;
    [SerializeField] 
    private KeyCode walkBackward = KeyCode.S;
    [SerializeField]
    private KeyCode walkLeft = KeyCode.A;
    [SerializeField]
    private KeyCode walkRight = KeyCode.D;
    [SerializeField]
    private KeyCode sprint = KeyCode.LeftShift;
    [SerializeField]
    private KeyCode jump = KeyCode.Space;

    public Vector2 GetLookDirection() {
        return new Vector3(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }
    public bool isWalkForward() { return Input.GetKey(walkForward); }
    public bool isWalkBackward() { return Input.GetKey(walkBackward); }
    public bool isWalkLeft() { return Input.GetKey(walkLeft); }
    public bool isWalkRight() { return Input.GetKey(walkRight); }
    public bool isSprint() { return Input.GetKey(sprint); }
    public bool isJump() { return Input.GetKeyDown(jump); }
}
