using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    // Range is a float indicating the range in which the player can interact with an object
    private float range = 10;

    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            // Creates Ray object from camera pointing to a point on the screen
            // that corresponds to the player's mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // The ray variable indicates the origin of the ray, the hit variable is where the
            // information about what the ray hits will be stored, and the range variable represents
            // the numerical range in which an object can be interacted with
            if (Physics.Raycast(ray, out RaycastHit hit, range))
            // If an object is within the range and is hit by the raycast...
            {
                if (hit.collider.gameObject.TryGetComponent(out Interact interactableObject))
                // If the object hit has an Interactable component...
                {
                    // Passes in the current gameObject this script is attached to
                    interactableObject.Grab(gameObject);
                }
            }
        }
    }
}
