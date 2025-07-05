using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private PlayerHealth playerHealth;
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        TogglePlayerMovement();
    }

    private void TogglePlayerMovement()
    {
        playerMovement.enabled = playerHealth.currentHealth > 0;
        playerMovement.hasBatteryForJumpAndSprint = playerHealth.currentHealth > playerHealth.dangerHealth;
        playerMovement.walkSpeed = playerHealth.currentHealth > playerHealth.dangerHealth ? playerMovement.walkSpeed : playerMovement.crouchSpeed;
    }
}
