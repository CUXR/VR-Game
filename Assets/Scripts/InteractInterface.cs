using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface Interactable {

    // This function is called by the player being in range of an interactable object and pressing the
    // "e" key. As long as the player holds the key, that is the amount of time that the object is 
    // being interacted with. The effects of the interation depend on the characteristics of the object.
    // Takes in a GameObject (usually the player) as an argument.
        public void Grab(GameObject gObject);
}

