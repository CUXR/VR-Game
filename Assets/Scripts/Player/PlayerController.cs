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
        else if (playerHealth.isPlayerInDanger()) // player on low health
        {
            cameraFollow.enabled = true;
            playerMovement.enabled = true;
            playerMovement.hasBatteryForJumpAndSprint = false; // disable jump and sprint (not enough battery)
        }
        else
        {
            cameraFollow.enabled = true;
            playerMovement.enabled = true;
            playerMovement.hasBatteryForJumpAndSprint = true; // enable jump and sprint
        }

        if (playerBackpack.isOpen || GameController.Instance.isPaused)
        {
            cameraFollow.enabled = false;
            playerMovement.enabled = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None; // player can use mouse to select options
        }
        else
        {
            cameraFollow.enabled = true;
            playerMovement.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}