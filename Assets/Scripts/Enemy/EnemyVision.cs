using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class EnemyVision : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 7f;
    public float peripheralRadius = 0.5f;

    [Range(0, 360)]
    public float viewAngle = 90f;

    [Header("Visibility Settings")]
    public float visibilityThreshold = 0.7f;
    public float visibilityIncreaseRate = 0.3f;
    public float visibilityPeripheralIncreaseRate = 0.1f;
    public float visibilityDecreaseRate = 0.1f;
    public float visibilityDistanceMultiplier = 2f;
    private float minVisibilityValue = 0f;
    private float maxVisibilityValue = 1f;
    private float visibilityValue;

    [Header("References")]
    private GameObject player;
    private Vector3 playerDirection;
    private float distanceToPlayer;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        // Check if player reference is set
        if (player == null)
            return;

        // Direction and distance from enemy to player
        playerDirection = (player.transform.position - transform.position).normalized;
        distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);

        bool inMainView = Vector3.Angle(transform.forward, playerDirection) <= viewAngle / 2;
        bool inPeripheral = distanceToPlayer <= peripheralRadius;

        // Check if player's direction is within viewing angle
        if (inMainView) {
            // Check if there's a clear line of sight to the player
            if (
                Physics.Raycast(
                    transform.position + Vector3.up * 0.5f, // Slightly raise the raycast origin
                    playerDirection,
                    out RaycastHit hit,
                    viewRadius
                )
            )
            {
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
        // Otherwise, check if the player is within the enemy's peripheral
        else if (inPeripheral) {
            visibilityValue = Mathf.Clamp(
                visibilityValue + visibilityPeripheralIncreaseRate * Time.deltaTime,
                minVisibilityValue,
                maxVisibilityValue
            );
        }
        else {
            visibilityValue = Mathf.Clamp(
                visibilityValue - visibilityDecreaseRate * Time.deltaTime,
                minVisibilityValue,
                maxVisibilityValue
            );
        }
    }

    public bool PlayerVisible()
    {
        return visibilityValue >= maxVisibilityValue;
    }

    private void OnDrawGizmos()
    {
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);

        if (player != null)
        {
            Gizmos.color = PlayerVisible() ? Color.green : Color.gray;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, player.transform.position);
        }
    }
}
