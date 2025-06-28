using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface Interactable
{

    // This function is called by the player being in range of an interactable object and pressing the
    // "e" key (applied only if the player is not currently holding an object). The effects of the
    // interaction depend on the characteristics of the object. Object generally remains in view of
    // the player. Takes in a GameObject (usually the player) as an argument.
    public void Grab(GameObject gObject);

    // This function is called when the player is holding an object and presses the "e" key. The
    // object is effectively released, and it is typically removed from the player's influence.
    public void Release();
}

