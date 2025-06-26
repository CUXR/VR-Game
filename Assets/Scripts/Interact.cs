using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    // When this function is called, the object's position is transformed to a point relative to the player's
    // position.
    public void Grab(GameObject gObject)
    {
        Vector3 myPosition = gObject.transform.position;

        float x = myPosition.x;
        float y = myPosition.y;
        float z = myPosition.z;

        transform.position = new Vector3(x, y, z);

    }
}

