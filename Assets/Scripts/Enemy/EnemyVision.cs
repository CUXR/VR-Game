using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class EnemyVision : MonoBehaviour
{
    [Header("References")]
    public Transform head;
    private GameObject player;
    private Vector3 playerDirection;
    private float distanceToPlayer;
    private EnemyController enemyController;

    [Header("Vision Settings")]
    public float viewRadius = 7f;
    public float peripheralRadius = 0.7f;

    [Range(0, 360)]
    public float viewAngle = 90f;

    [Header("Visibility Settings")]
    public float investigateThreshold = 0.4f;
    public float seenThreshold = 0.7f;
    public float visibilityIncreaseRate = 0.3f;
    public float visibilityPeripheralIncreaseRate = 0.1f;
    public float visibilityDecreaseRate = 0.1f;
    public float visibilityDistanceMultiplier = 2f;
    private float minVisibilityValue = 0f;
    private float maxVisibilityValue = 1f;
    private float visibilityValue;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        enemyController = GetComponent<EnemyController>();
    }

    void Update()
    {
        // Check if player reference is set
        if (player == null)
            return;

        // Direction and distance from enemy to player
        playerDirection = (player.transform.position - head.position).normalized;
        Vector3 enemyPos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
        distanceToPlayer = Vector3.Distance(playerPos, enemyPos);

        bool inMainView = Vector3.Angle(head.forward, playerDirection) <= viewAngle / 2;
        bool inPeripheral = distanceToPlayer <= peripheralRadius;


        // print("In Main View: " + inMainView + ", In Peripheral: " + inPeripheral + ", Distance: " + distanceToPlayer + ", Visibility: " + visibilityValue);

        // Check if player's direction is within viewing angle
        if (inMainView)
        {
            Debug.DrawRay(head.position, playerDirection * viewRadius, Color.red, Time.deltaTime);
            // Check if there's a clear line of sight to the player
            if (
                Physics.Raycast(
                    head.position,
                    playerDirection,
                    out RaycastHit hit,
                    viewRadius,
                    ~LayerMask.GetMask("Enemy") // Ignore other enemies in raycast
                )
            )
            {
                print("Hit: " + hit.collider.gameObject.name);
                // If the raycast hits the player, they are visible
                if (hit.collider.gameObject.CompareTag("Player"))
                {
                    visibilityValue = Mathf.Clamp(
                        visibilityValue + visibilityIncreaseRate * (1 + (viewRadius - hit.distance) / viewRadius * visibilityDistanceMultiplier) * Time.deltaTime,
                        minVisibilityValue,
                        maxVisibilityValue
                    );
                }
                else
                {
                    visibilityValue = Mathf.Clamp(
                        visibilityValue - visibilityDecreaseRate * Time.deltaTime,
                        minVisibilityValue,
                        maxVisibilityValue
                    );
                }
            }
        }
        else if (inPeripheral)
        {
            // Otherwise, check if the player is within the enemy's peripheral
            if (
                Physics.Raycast(
                    head.position,
                    playerDirection,
                    out RaycastHit hit,
                    viewRadius,
                    ~LayerMask.GetMask("Enemy") // Ignore other enemies in raycast
                )
            )
            {
                // If the raycast hits the player, they are visible
                if (hit.collider.gameObject.CompareTag("Player"))
                {
                    visibilityValue = Mathf.Clamp(
                        visibilityValue + visibilityPeripheralIncreaseRate * Time.deltaTime,
                        minVisibilityValue,
                        maxVisibilityValue
                    );
                }
            }
        }
        else
        {
            visibilityValue = Mathf.Clamp(
                visibilityValue - visibilityDecreaseRate * Time.deltaTime,
                minVisibilityValue,
                maxVisibilityValue
            );
        }

        if (visibilityValue >= investigateThreshold && visibilityValue < seenThreshold)
        {
            enemyController.AddInvestigatePosition(player.transform.position);
            print("Investigate Position Added [VISION]");
        }
    }

    public bool PlayerVisible()
    {
        return visibilityValue >= maxVisibilityValue;
    }

    public bool PlayerInvestigate()
    {
        return visibilityValue >= investigateThreshold && visibilityValue < seenThreshold;
    }

    private void OnDrawGizmos()
    {
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(head.position, head.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(head.position, head.position + rightBoundary * viewRadius);

        // Peripheral vision radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(head.position, peripheralRadius);

        // Main vision radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(head.position, viewRadius);

        if (player != null)
        {
            Gizmos.color = PlayerVisible() ? Color.green : Color.gray;
            Gizmos.DrawLine(head.position, player.transform.position);
        }
    }
}
