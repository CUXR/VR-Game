using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Boolean holding = false;

    private float width;

    private float x;
    private float y;
    private float z;

    GameObject player;


    // Object's collider is disabled, and the holding variable is 
    // updated.
    public void Grab(GameObject gObject)
    {
        player = gObject;
        Collider collider = GetComponent<Collider>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        Vector3 size = renderer.bounds.size;
        width = size.x;

        collider.enabled = false;

        holding = true;

    }

    // Collider is enabled and holding is updated to false.
    public void Release()
    {
        Collider collider = GetComponent<Collider>();
        collider.enabled = true;
        holding = false;
    }

    // If holding is true, then object's position is updated to a new position relative to the player's view.
    void Update()
    {
        if (holding == true)
        {
            Vector3 myPosition = player.transform.GetChild(0).GetChild(0).position;

            x = myPosition.x;
            y = myPosition.y;
            z = myPosition.z;
            transform.position = new Vector3(x, y, z + width);
        }
    }
}

