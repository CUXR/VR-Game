using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Boolean holding = false;

    private Vector3 width;

    private Transform hold;

    private float throwForce;

    private Rigidbody rb;

    private Renderer rend;

    void Start()
    {
        throwForce = 15.0f;
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Holding variable is updated to true, and object's layer is changed to layer 6
    public void Grab(GameObject gObject)
    {
        gameObject.layer = 6;
        hold = Camera.main.transform.GetChild(0);
        rend = gameObject.GetComponent<Renderer>();
        width = rend.bounds.size;
        Vector3 offset = hold.InverseTransformVector(hold.right * (0.7f * width.x)
        + hold.up * (0.7f * -width.y) + hold.forward * (0.7f * width.z));
        rb.useGravity = false;
        rb.isKinematic = true;
        transform.SetParent(hold);
        transform.localPosition = offset;
        transform.localRotation = Quaternion.identity;
        GetComponent<Collider>().enabled = false;
        transform.localScale = Vector3.one;
    }

    // Holding is updated to false, and object's layer is changed to 0.
    public void Release()
    {
        gameObject.layer = 0;
        gameObject.transform.SetParent(null);
        rb.useGravity = true;
        rb.isKinematic = false;
        GetComponent<Collider>().enabled = true;
    }

    public void Throw()
    {
        Release();
        rb.AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);
    }

}

