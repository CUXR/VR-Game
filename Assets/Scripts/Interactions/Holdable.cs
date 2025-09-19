using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

public class Holdable : MonoBehaviour, Interactable
{
    private Vector3 holdOffset;
    private Vector3 targetPos;
    private Transform holdPosition;
    private bool holding;
    private bool objectPressed;
    private Vector3 size;
    private float rangeTimer = 0f;
    private float radiusToDraw;
    private float soundTimer = 0f;
    private float soundRadiusDuration = 2f;

    [Header("Serialized Fields")]
    [SerializeField] private float maxDistance = 1.5f;
    [SerializeField] private float throwForce = 15.0f;
    [SerializeField] private float maxRangeTime = 0.5f;
    [SerializeField] private float horizontalOffset = 1.1f;
    [SerializeField] private float verticalOffset = 0.2f;
    [SerializeField] private float forwardOffset = 1.5f;

    [Header("Component References")]
    private Rigidbody rb;
    private Collider playerCollider;

    [Header("Sound Variables")]
    private float objectVolumeRadius = 7f;
    private float objectVolumeDecay = 0.4f;
    private float objectLoudness = 0.3f;


    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        size = gameObject.GetComponent<Renderer>().bounds.size;
        playerCollider = GameObject.FindWithTag("Player").GetComponent<Collider>();
    }

    public bool IsHoldable()
    {
        return true;
    }

    public bool IsThrowable()
    {
        return true;
    }

    public void InteractWith()
    {
        // This implementation of InteractWith allows the player to grab and hold the object (InteractInterface)
        holdPosition = Camera.main.transform.GetChild(0);
        // Calculates an offset based off of holdPosition so that the object is held in player view
        holdOffset = holdPosition.InverseTransformVector((holdPosition.right * horizontalOffset) +
            (holdPosition.forward * size.z * forwardOffset)
            + (holdPosition.up * size.y * verticalOffset));
        rb.useGravity = false;
        rb.freezeRotation = true;
        transform.SetParent(holdPosition);
        transform.rotation = Camera.main.transform.rotation;
        targetPos = holdPosition.TransformPoint(holdOffset);
        rb.MovePosition(targetPos);
        holding = true;
    }

    public void Release()
    {
        // This implementation of Release drops the object (InteractInterface)
        transform.SetParent(null);
        rb.useGravity = true;
        rb.freezeRotation = false;
        holding = false;
    }

    public void Throw()
    {
        // This function is called when the player is holding an object and presses the left mouse
        // button. The object is sent in a direction away from the player at a velocity determined by
        // a throwForce variable.
        Release();
        rb.AddForce(Camera.main.transform.forward * throwForce + Camera.main.transform.up * throwForce * 0.5f, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (holding && collision.collider.tag != "Player")
        {
            objectPressed = true;
        }
        float kineticEnergy = 0.5f * rb.mass * Mathf.Pow(collision.relativeVelocity.magnitude, 2);
        // Sound produced by object hitting something
        AudioUtility.SoundProduced(new Sound(transform.position, objectVolumeRadius * kineticEnergy,
            objectLoudness * kineticEnergy, objectVolumeDecay));
        radiusToDraw = objectVolumeRadius * kineticEnergy;
        soundTimer = soundRadiusDuration;
    }

    void OnCollisionExit(Collision collision)
    {
        if (holding && collision.collider.tag != "Player")
        {
            objectPressed = false;
        }
    }

    void FixedUpdate()
    {
        if (holding)
        {
            AdjustHoldPosition();
        }
        if (soundTimer > 0f)
        {
            soundTimer -= Time.fixedDeltaTime;
            if (soundTimer <= 0f)
            {
                radiusToDraw = 0f;
            }
        }
    }

    public void AdjustHoldPosition()
    {
        // This function is called in FixedUpdate() to ensure that the held object
        // interacts correctly with walls and other objects (physics-wise).
        targetPos = holdPosition.TransformPoint(holdOffset);
        Vector3 currentPos = transform.position;
        Vector3 toTarget = targetPos - currentPos;
        if (objectPressed)
        {
            rb.velocity = (targetPos - currentPos) * 1.2f;
        }
        else
        {
            rb.velocity = (targetPos - currentPos) * 5f;
        }
        // More forgiving interaction system, allowing a bit of time out of range
        // before object is released
        if (toTarget.magnitude > maxDistance)
        {
            rangeTimer += Time.fixedDeltaTime;
            if (rangeTimer > maxRangeTime)
            {
                Release();
            }
        }
        else
        {
            rangeTimer = 0f;
        }
        // If object being held leaves player's view, object is released
        Vector3 toCamera = Camera.main.transform.position - transform.position;
        if (Physics.Raycast(transform.position,
               toCamera.normalized, out RaycastHit hit, toCamera.magnitude))
        {
            if (hit.collider != playerCollider)
            {
                Release();
            }
        }

    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radiusToDraw);
    }
}

