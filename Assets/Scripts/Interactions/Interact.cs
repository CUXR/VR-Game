using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Vector3 holdOffset;
    private Transform holdPosition;
    private bool holding;
    private bool wallpress;
    private Vector3 size;
    private float maxDistance = 1.5f;
    private float throwForce = 15.0f;
    [Header("Component References")]
    private Rigidbody rb;
    private Collider objectCollider;
    private Collider playerCollider;
    

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        objectCollider = gameObject.GetComponent<Collider>();
        size = gameObject.GetComponent<Renderer>().bounds.size;
    }

    public void Grab(Collider playerCollider)
    {
        holdPosition = Camera.main.transform.GetChild(0);
        holdOffset = holdPosition.InverseTransformVector((holdPosition.forward * size.z * 0.5f) +
            (holdPosition.up * -size.y * 0.3f));
        rb.useGravity = false;
        rb.freezeRotation = true;
        transform.SetParent(holdPosition);
        transform.localScale = Vector3.one;
        holding = true;
        Physics.IgnoreCollision(objectCollider, playerCollider, true);
        Vector3 targetPos = holdPosition.TransformPoint(holdOffset);
        Vector3 halfExtents = size * 0.5f;
        Quaternion rotation = Quaternion.identity;
        Collider[] hits = Physics.OverlapBox(targetPos, halfExtents, rotation);
        bool blocked = false;
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player"))
                {
                    blocked = true;
                    break;
                }
        }
        if (!blocked)
        {
            transform.position = targetPos;
        }
        else
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPos);
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance))
            {
                if (!hit.collider.CompareTag("Player"))
                {
                    transform.position = hit.point - direction * 0.05f;
                }
                else
                {
                    transform.position = targetPos;
                }
            }
            else
            {
                transform.position = targetPos;
            }
        }
    }

    public void Release()
    {
        transform.SetParent(null);
        rb.useGravity = true;
        rb.freezeRotation = false;
        holding = false;
        Physics.IgnoreCollision(objectCollider, playerCollider, false);
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
            wallpress = true;
            Debug.Log("wall");
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (holding && collision.collider.tag != "Player")
        {
            wallpress = false;
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
        Vector3 targetPos = holdPosition.TransformPoint(holdOffset);
        Vector3 currentPos = transform.position;
        if (wallpress)
        {
            Vector3 pushBack = (targetPos - currentPos) * 2.0f;
            rb.velocity = pushBack;
        }
        else
        {
            Vector3 toTarget = targetPos - currentPos;
            rb.velocity = toTarget * 10f;
        }
        if (Vector3.Distance(transform.position, holdPosition.position) > maxDistance) {
            Release();
        }
    }
}

