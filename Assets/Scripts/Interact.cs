using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Boolean holding = false;

    private Vector3 width;

    private float x;
    private float y;
    private float z;

    public float throwForce;

    public Rigidbody rb;

    GameObject player;

    void Start()
    {
        throwForce = 15.0f;
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Holding variable is updated to true, and object's layer is changed to layer 6
    public void Grab(GameObject gObject)
    {
        player = gObject;
        holding = true;
        gameObject.layer = 6;
        Renderer renderer = GetComponent<Renderer>();
        width = renderer.bounds.size;
    }

    // Holding is updated to false, and object's layer is changed to 0.
    public void Release()
    {
        holding = false;
        gameObject.layer = 0;
    }

    public void Throw()
    {
        Release();
        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * 5f, Color.red, 2f);
        rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);
    }

    // If holding is true, then object's position is updated to a new position relative to the player's view.
    void LateUpdate()
    {
        if (holding == true)
        {
            Transform camera = player.transform.GetChild(0).GetChild(0);
            // Offset relative to the camera's view direction
            Vector3 offset = camera.right * (0.7f * width.x) + camera.up * (0.7f * -width.y) +
            camera.forward * (0.7f * width.z);
            transform.position = camera.position + offset;
            transform.rotation = Quaternion.LookRotation(camera.forward, camera.up);
        }
    }
}

