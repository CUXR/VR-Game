using UnityEngine;

public class ObjectInteraction : MonoBehaviour
{
    // Range is a float indicating the range in which the player can interact with an object
    public float range = 3;

    // Scale for how much force to push a movable object
    public float pushForce;

    // Object currently being held; if nothing, is null
    private Holdable held = null;

    // Object last highlighted
    private InteractableInterface lastHighlighted;

    void Update()
    {
        // The ray is determined by the camera position, the hit variable is where the
        // information about what the ray hits will be stored, and the range variable represents
        // the numerical range in which an object can be interacted with
        if (Physics.Raycast(Camera.main.gameObject.transform.position,
            Camera.main.gameObject.transform.forward, out RaycastHit hit, range))
            // If an object is within the range, is hit by the raycast
        {
            if (hit.collider.gameObject.TryGetComponent(out InteractableInterface interactableObject))
                // If the object hit has an Interactable component
            {
                if (lastHighlighted != interactableObject)
                {
                    lastHighlighted?.SetGlow(false);
                    interactableObject.SetGlow(true);
                    lastHighlighted = interactableObject;
                }
                if (InputController.Instance.GetInteractDown())
                {
                    if (interactableObject is Movable movableObject)
                    {
                        movableObject.SetInteractor(gameObject.transform, pushForce);
                    }
                    if (held == null && interactableObject is Holdable holdableObject)
                    {
                        held = holdableObject;
                    }
                    interactableObject.Interact();
                } else
                {
                    if (held!=null)
                    {
                        held.Release();
                        held = null;
                    }
                }
            }

        }
        if (Input.GetKeyDown(KeyCode.Mouse0) && held != null && held is Holdable interact)
        {
            interact.Throw();
            held = null;
        }
    }
}