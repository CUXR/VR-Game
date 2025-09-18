using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface Interactable
{

    // This function is called by the player being in range of an interactable object and pressing the
    // "e" key (applied only if the player is not currently holding an object). The effects of the
    // interaction depend on the characteristics of the object. Takes in a collider (player collider)
    // as well as three floats that determine the offset in the player's camera view.
    public void InteractWith();

    // This function is called when the player is holding an object and presses the "e" key. The
    // object is effectively released and is typically removed from the player's influence.
    public void Release();

    // This function is called to check if the Interactable is holdable.
    public bool IsHoldable();
}

