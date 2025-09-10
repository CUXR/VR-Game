using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface Interactable
{

    // This function is called by the player being in range of an interactable object and pressing the
    // "e" key (applied only if the player is not currently holding an object). The effects of the
    // interaction depend on the characteristics of the object. Takes in a GameObject (the player)
    // as an argument.
    public void Grab(Collider collider, float horizontalOffset, float verticalOffset,
        float forwardOffset);

    // This function is called when the player is holding an object and presses the "e" key. The
    // object is effectively released and is typically removed from the player's influence.
    public void Release();

    // This function is called when the player is holding an object and presses the left mouse
    // button. The object is sent in a direction away from the player at a velocity determined by
    // a throwForce variable.
    public void Throw();

    // This function is called in FixedUpdate() for any Interactable to ensure that the held object
    // interacts correctly with walls and other objects (physics-wise).
    void AdjustHoldPosition();
}

