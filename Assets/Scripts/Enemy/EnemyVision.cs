using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class EnemyVision : MonoBehaviour
{
    [Header("Vision Settings")]
    public float viewRadius = 7f;

    [Range(0, 360)]
    public float viewAngle = 90f;
    public bool playerVisible;

    GameObject player;
    Vector3 playerDirection;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        // Check if player reference is set
        if (player == null)
            return;

        // Direction from enemy to player
        playerDirection = (player.transform.position - transform.position).normalized;

        // Check if player's direction is within viewing angle
        if (Vector3.Angle(transform.forward, playerDirection) > viewAngle / 2)
            return;

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
                playerVisible = true;
            }
            else
            {
                playerVisible = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, playerDirection * viewRadius);
    }
}
