using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
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
        holding = true;
        player.TryGetComponent(out Rigidbody rb);
        gameObject.layer = 6;
    }

    // Collider is enabled and holding is updated to false.
    public void Release()
    {
        Collider collider = GetComponent<Collider>();
        holding = false;
        player.TryGetComponent(out Rigidbody rb);
        gameObject.layer = 0;
    }

    // If holding is true, then object's position is updated to a new position relative to the player's view.
    void Update()
    {
        if (holding == true)
        {
            Transform camera = player.transform.GetChild(0);
            // Offset relative to the camera's view direction
            TryGetComponent(out Rigidbody rb);
            Vector3 offset = camera.right * 0.3f - camera.up * 0.1f + camera.forward * 0.7f;
            transform.position = camera.position + offset;
            transform.rotation = Quaternion.LookRotation(camera.forward, camera.up);
            //rb.MovePosition(camera.position + offset);
            //rb.MoveRotation(Quaternion.LookRotation(camera.forward, camera.up));
        }
    }
}

