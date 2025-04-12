using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private InputController inputController;
    private Rigidbody rb;

    [SerializeField]
    private float moveSpeed;
    private Vector3 direction;

    [SerializeField]
    private float sprintSpeed;
    void Start()
    {
        inputController = GetComponent<InputController>();
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        ClampVelocity();
    }

    private void Move()
    {
        direction = (
            transform.right * (inputController.isWalkRight() ? 1 : (inputController.isWalkLeft() ? -1 : 0)) +
            transform.forward * (inputController.isWalkForward() ? 1 : (inputController.isWalkBackward() ? -1 : 0))
        ).normalized;

        rb.AddForce(direction * (inputController.isSprint() ? sprintSpeed : moveSpeed));
    }

    void ClampVelocity() {
       Vector3 rawVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

        // Clamp x and z axis velocity
        if (rawVelocity.magnitude > (inputController.isSprint() ? sprintSpeed : moveSpeed)) {
            Vector3 clampedVelocity = rawVelocity.normalized * (inputController.isSprint() ? sprintSpeed : moveSpeed);
            rb.velocity = new Vector3(clampedVelocity.x, rb.velocity.y, clampedVelocity.z);
        }
    }
}
