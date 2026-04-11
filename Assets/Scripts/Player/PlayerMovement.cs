using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerMovement : MonoBehaviour
{
    public enum MovementState
    {
        IDLE,
        WALK,
        SPRINT,
        CROUCH,
        AIR,
        CRAWL,
    }

    [Header("Movement")]
    public bool hasBatteryForJumpAndSprint;
    public float sprintSpeed;
    public float walkSpeed;
    public float wallRunSpeed;
    public float groundDrag;
    public MovementState movementState;
    private float moveSpeed;

    [Header("Jump")]
    public float jumpForce;
    public float airSpeed;
    public float airMultiplier;
    public float coyoteTime;
    public float jumpBuffer;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    [Header("Crouch")]
    public Volume volume;
    public float crouchSpeed;
    public float crouchScale;
    public float upDetectionHeight;
    private float downDetectionHeight = 0.2f;
    public float defaultVignette,
        crouchVignette;
    private float defaultScale;
    private Vignette vignette;
    private float radiusToDraw;

    [Header("Crawl")]
    public float crawlSpeed;
    public float crawlScale;
    public float crawlUpDetectionHeight;
    public float crawlVignette;
    private bool crawling = false;

    [Header("Slope Check")]
    public float maxSlopeAngle;
    public bool Grounded { get; private set; }
    RaycastHit slopeHit;
    public float playerHeight { get; private set; }
    bool exitingSlope;

    [Header("Step Check")]
    public bool stepClimbEnabled;
    public GameObject rayLower;
    public GameObject rayUpper;
    public float stepHeight;
    public float stepSmoothing;

    [Header("Walking Sound Variables")]
    private float walkingVolumeRadius = 13f;
    private float walkingVolumeDecay = 0.4f;
    private float walkingLoudness = 0.6f;

    [Header("Sprinting Sound Variables")]
    private float sprintingVolumeRadius = 20f;
    private float sprintingVolumeDecay = 0.1f;
    private float sprintingLoudness = 1.0f;

    Rigidbody rb;
    Vector3 moveDirection;
    float horizontalInput,
        verticalInput;
    private PlayerLimb playerLimb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        playerHeight =
            GetComponentInChildren<CapsuleCollider>().height * gameObject.transform.localScale.y;

        hasBatteryForJumpAndSprint = true;

        if (!volume.profile.TryGet(out vignette))
        {
            Debug.LogWarning("No Vignette component found on Global Volume");
        }

        rayUpper.transform.position = rayLower.transform.position + stepHeight * Vector3.up;
        defaultScale = transform.localScale.y;
        playerLimb = GetComponent<PlayerLimb>();
    }

    void Update()
    {
        // Check if is grounded
        Grounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            playerHeight * 0.5f + downDetectionHeight,
            ~0,
            QueryTriggerInteraction.Ignore
        );
        exitingSlope = !Grounded;

        GetInput();
        SpeedControl();
        SetDrag();
        HandleMovementState();
    }

    void FixedUpdate()
    {
        Move();

        if (stepClimbEnabled)
        {
            StepClimb();
        }
    }

    void GetInput()
    {
        // Check input and set scales/movement state
        Vector2 movement = InputController.Instance.GetWalkDirection();
        horizontalInput = movement.x;
        verticalInput = movement.y;
        PreJumpCheck();
        // if player does not have two legs or does not have enough
        // space above to get out of crawl, then crawl
        bool mustCrawl = playerLimb.equippedLimbs.Count == 2 || Physics.Raycast(
            transform.position,
            Vector3.up,
            playerHeight * 0.5f + crawlUpDetectionHeight,
            ~0, 
            QueryTriggerInteraction.Ignore); // ignores trigger collisions
        // if player does not have two legs or does not have enough
        // space above to get out of crawl, then crawl
        if (Grounded) {
            if (mustCrawl)
            {
                crawling = true;  
            } else if (InputController.Instance.GetCrawlDown())
            {
                // toggle crawling
                crawling = !crawling;
            } else if (
                coyoteTimeCounter > 0f
                && jumpBufferCounter > 0f
                && movementState != MovementState.CROUCH
                && movementState != MovementState.CRAWL
                && hasBatteryForJumpAndSprint
            )
            {
                Jump();
                // Reset jump buffer to prevent jumping again
                jumpBufferCounter = 0f;
            }

            else if (!crawling && (Physics.Raycast(
                transform.position,
                Vector3.up,
                playerHeight * 0.5f + upDetectionHeight,
                ~0,
                QueryTriggerInteraction.Ignore) ||
                InputController.Instance.GetCrouchDown() ||
                InputController.Instance.GetCrouchHold()))
            {
                // something above player or crouching, then crouch
                    movementState = MovementState.CROUCH;
            } else if (InputController.Instance.GetSprint() &&
                InputController.Instance.GetWalkDirection().magnitude > 0 &&
                hasBatteryForJumpAndSprint)
            {
                movementState = MovementState.SPRINT;
            } else if (InputController.Instance.GetWalkDirection().magnitude > 0)
            {
                movementState = MovementState.WALK;
            } else
            {
                movementState = MovementState.IDLE;
            }
            if (crawling)
            {
                movementState = MovementState.CRAWL;
            }
        } else {
            movementState = MovementState.AIR;
        }
    }

    void PreJumpCheck()
    {
        // Coyote time check
        if (Grounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        // Jump buffer check
        if (InputController.Instance.GetJumpDown())
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    void HandleMovementState()
    {
        // set variables based on movement state
        if (movementState == MovementState.AIR)
        {
            moveSpeed = airSpeed;
        } else if (movementState == MovementState.CRAWL)
        {
            radiusToDraw = 0;
            moveSpeed = crawlSpeed;
            vignette.intensity.value = crawlVignette;
            transform.localScale = new Vector3(
                transform.localScale.x,
                crawlScale,
                transform.localScale.z);
        } else if (movementState == MovementState.CROUCH)
        {
            transform.localScale = new Vector3(
                transform.localScale.x,
                crouchScale,
                transform.localScale.z);
            radiusToDraw = 0;
            moveSpeed = crouchSpeed;
            vignette.intensity.value = crouchVignette;
        } else
        {
            transform.localScale = new Vector3(
                transform.localScale.x,
                defaultScale,
                transform.localScale.z);
            vignette.intensity.value = defaultVignette;
            if (movementState == MovementState.SPRINT)
            {
                // Sound produced by sprinting
                AudioUtility.SoundProduced(new Sound(transform.position, sprintingVolumeRadius, sprintingLoudness, sprintingVolumeDecay));
                radiusToDraw = sprintingVolumeRadius;
                moveSpeed = sprintSpeed;
            } else if (movementState == MovementState.WALK)
            {
                // Sound produced by walking
                AudioUtility.SoundProduced(new Sound(transform.position, walkingVolumeRadius, walkingLoudness, walkingVolumeDecay));
                radiusToDraw = walkingVolumeRadius;
                moveSpeed = walkSpeed;
            } else {
                // idle
                moveSpeed = 0;
                radiusToDraw = 0;
            }
        }

        float limbMultiplier = playerLimb != null ? playerLimb.moveSpeedMultiplier : 1f;
        moveSpeed *= limbMultiplier;
    }

    void Move()
    {
        moveDirection = (
            transform.right * horizontalInput + transform.forward * verticalInput
        ).normalized;

        // Apply force perpendicular to slope's normal if on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(20 * moveSpeed * GetSlopeMoveDirection(), ForceMode.Force);

            // Apply downward force to keep player on slope
            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(
                    Vector3.down
                        * ((movementState == MovementState.CROUCH || 
                        movementState == MovementState.CRAWL) ? 10f : 20f)
                       ,
                    ForceMode.Force
                );
            }
        }
        // Move in direction
        else if (Grounded)
        {
            rb.AddForce(10 * moveSpeed * moveDirection, ForceMode.Force);
        }
        // Move in direction but slower in air
        else if (!Grounded)
        {
            rb.AddForce(
                10 * moveSpeed * moveDirection * airMultiplier,
                ForceMode.Force
            );
        }
    }

    void SetDrag()
    {
        if (movementState == MovementState.AIR)
        {
            rb.linearDamping = 0;
        }
        else
        {
            rb.linearDamping = groundDrag;
        }
    }

    void SpeedControl()
    {
        // Prevents player from exceeding move speed on slopes
        if (OnSlope() && !exitingSlope && rb.linearVelocity.magnitude > moveSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
        }
        else
        {
            Vector3 rawVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

            // Clamp x and z axis velocity
            if (rawVelocity.magnitude > moveSpeed)
            {
                Vector3 clampedVelocity = rawVelocity.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(clampedVelocity.x, rb.linearVelocity.y, clampedVelocity.z);
            }
        }
    }

    void Jump()
    {
        exitingSlope = true;

        // Sound produced by jumping
        AudioUtility.SoundProduced(new Sound(transform.position, sprintingVolumeRadius, sprintingVolumeDecay, sprintingLoudness));
        radiusToDraw = sprintingVolumeRadius;

        // Resets y-velocity to have consistent jump height
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    void StepClimb()
    {
        Debug.DrawRay(rayLower.transform.position, rayLower.transform.forward * 0.1f, Color.red);
        Debug.DrawRay(rayUpper.transform.position, rayUpper.transform.forward * 0.2f, Color.red);

        if (
            Physics.Raycast(rayLower.transform.position, rayLower.transform.forward, out _, 0.15f)
            && !Physics.Raycast(
                rayUpper.transform.position,
                rayUpper.transform.forward,
                out _,
                0.3f
            )
        )
        {
            rb.position += new Vector3(0f, stepSmoothing, 0f);
        }
    }

    bool OnSlope()
    {
        if (
            Physics.Raycast(
                transform.position,
                Vector3.down,
                out slopeHit,
                playerHeight * 0.5f + 0.3f
            )
        )
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);

            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    public Vector3 GetMoveVelocity()
    {
        if (rb == null)
        {
            return Vector3.zero;
        }

        return new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radiusToDraw);
    }
}
