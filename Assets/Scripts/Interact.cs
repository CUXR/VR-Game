using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Boolean holding = false;

    public float width;

    private float x;
    private float y;
    private float z;

    GameObject player;

    private float rotationX, rotationY;
    [SerializeField]
    private float lookSpeed;


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
            Transform camera = player.transform.GetChild(0);
            // Offset relative to the camera's view direction
            Vector3 offset = camera.right * 0.4f - camera.up * 0.3f + camera.forward * 0.7f;
            transform.position = camera.position + offset;
            transform.rotation = Quaternion.LookRotation(camera.forward, camera.up);
        }
    }
}

