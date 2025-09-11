using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Vector3 holdOffset;
    private Vector3 targetPos;
    private Transform holdPosition;
    private bool holding;
    private bool objectPressed;
    private Vector3 size;
    private float rangeTimer = 0f;
    [Header("Serialized Fields")]
    [SerializeField] private float maxDistance = 1.5f;
    [SerializeField] private float throwForce = 15.0f;
    [SerializeField] private float maxRangeTime = 0.5f;

    [Header("Component References")]
    private Rigidbody rb;
    private Collider playerColliderRef;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        size = gameObject.GetComponent<Renderer>().bounds.size;
    }

    public void Grab(Collider playerCollider, float horizontalOffset, float verticalOffset,
        float forwardOffset)
    {
        playerColliderRef = playerCollider;
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
        transform.SetParent(null);
        rb.useGravity = true;
        rb.freezeRotation = false;
        holding = false;
    }

    public void Throw()
    {
        Release();
        rb.AddForce(Camera.main.transform.forward * throwForce + Camera.main.transform.up * throwForce * 0.5f, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (holding && collision.collider.tag != "Player")
        {
            objectPressed = true;
        }
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
    }

    public void AdjustHoldPosition()
    {
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
            if (hit.collider != playerColliderRef)
            {
                Release();
            }
        }
    }
}

