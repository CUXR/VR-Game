using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Callbacks;
using UnityEngine;

public class Interact : MonoBehaviour, Interactable
{
    private Vector3 holdOffset;
    private Transform hold;
    private float throwForce;
    private Rigidbody rb;
    private Collider objectCollider;
    private Renderer rend;
    private Collider playerCollider;
    private bool holding;
    private bool wallpress;
    private Vector3 size;
    private float maxDistance = 1.5f;

    void Start()
    {
        throwForce = 15.0f;
        rb = gameObject.GetComponent<Rigidbody>();
        objectCollider = gameObject.GetComponent<Collider>();
        rend = gameObject.GetComponent<Renderer>();
        size = rend.bounds.size;
    }

    public void Grab(GameObject gObject)
    {
        hold = Camera.main.transform.GetChild(0);
        holdOffset = hold.InverseTransformVector(hold.right * (0.7f * size.x) +
            hold.up * (0.7f * -size.y) + hold.forward * (0.7f * size.z));
        rb.useGravity = false;
        rb.freezeRotation = true;
        transform.SetParent(hold);
        transform.localScale = Vector3.one;
        holding = true;
        playerCollider = gObject.GetComponent<Collider>();
        Physics.IgnoreCollision(objectCollider, playerCollider, true);
        Vector3 targetPos = hold.TransformPoint(holdOffset);
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
        Vector3 targetPos = hold.TransformPoint(holdOffset);
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
        if (Vector3.Distance(transform.position, hold.position) > maxDistance) {
            Release();
        }
    }
}

