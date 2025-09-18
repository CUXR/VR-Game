using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    // Range is a float indicating the range in which the player can interact with an object
    public float range = 3;

    // Object currently being held; if nothing, is null
    private Interactable held = null;

    private Collider playerCollider;

    void Start()
    {
        playerCollider = gameObject.GetComponent<Collider>();
    }

    void Update()
    {
        if (InputController.Instance.GetInteractDown())
        {
            if (held == null)
            {
                // The ray is determined by the camera position, the hit variable is where the
                // information about what the ray hits will be stored, and the range variable represents
                // the numerical range in which an object can be interacted with
                if (Physics.Raycast(Camera.main.gameObject.transform.position,
                Camera.main.gameObject.transform.forward, out RaycastHit hit, range))
                // If an object is within the range, is hit by the raycast, and has Interact component
                {
                    if (hit.collider.gameObject.TryGetComponent(out Rigidbody rb))
                    {
                        if (hit.rigidbody.gameObject.TryGetComponent(out Holdable interactableObject))
                        // If the object hit has an Interactable component
                        {
                            interactableObject.InteractWith();
                            if (interactableObject.IsHoldable())
                            {
                                held = interactableObject;
                            }
                        }
                    }
                }
            }
            else
            {
                held.Release();
                held = null;
            }

        }
        if (Input.GetKeyDown(KeyCode.Mouse0) && held != null && held is Holdable interact)
        {
            interact.Throw();
            held = null;
        }
    }
}
