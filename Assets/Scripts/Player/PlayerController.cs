using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private CameraFollow cameraFollow;
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    private PlayerBackpack playerBackpack;

    void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
        playerBackpack = GetComponent<PlayerBackpack>();
    }

    void Update()
    {
        if (playerHealth.isPlayerDead())
        {
            cameraFollow.enabled = false;
            playerMovement.enabled = false;
            playerBackpack.isOpen = false; // Close backpack if player is dead
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            return;
        }

        if (playerHealth.isPlayerInDanger())
        {
            cameraFollow.enabled = true;
            playerMovement.enabled = true;
            playerMovement.hasBatteryForJumpAndSprint = false; // Disable jump and sprint if in danger
        }

        if (playerBackpack.isOpen)
        {
            cameraFollow.enabled = false;
            playerMovement.enabled = false;
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            cameraFollow.enabled = true;
            playerMovement.enabled = true;
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
