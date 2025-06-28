using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public Color defaultHealthColor;
    public Color dangerHealthColor;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float dangerHealth = 20f;
    public float minHealth = 0f;
    public float defaultDecayRate = 2f;

    public float regenAmount = 50f;

    [HideInInspector]
    public float currentHealth;
    private PlayerMovement playerMovement;


    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        currentHealth = 100f;
        SyncSliderHealth();
    }

    void Update()
    {
        Debug.Log("Update running");
        float decayMultiplier = 1f;

        switch (playerMovement.movementState)
        {
            case PlayerMovement.MovementState.IDLE:
                decayMultiplier = 1f;
                break;
            case PlayerMovement.MovementState.WALK:
            case PlayerMovement.MovementState.CROUCH:
                decayMultiplier = 1.5f;
                break;
            case PlayerMovement.MovementState.SPRINT:
            case PlayerMovement.MovementState.WALLRUN:
            case PlayerMovement.MovementState.AIR:
                decayMultiplier = 2f;
                break;
        }

        // Apply the multiplier to health decay
        currentHealth = Mathf.Clamp(
            currentHealth - defaultDecayRate * decayMultiplier * Time.deltaTime,
            minHealth,
            maxHealth
        );
        SyncSliderHealth();
    }
    void SyncSliderHealth()
    {
        healthSlider.value = currentHealth;

        // Set the fill color based on current health
        healthSlider.fillRect.GetComponent<Image>().color =
            currentHealth <= dangerHealth ? dangerHealthColor : defaultHealthColor;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Battery"))
        {
            // Increase health by regen amount when picking up the battery
            currentHealth = Mathf.Clamp(currentHealth + regenAmount, minHealth, maxHealth);
            SyncSliderHealth();
            Destroy(other.gameObject); // Remove the health pickup from the scene
        }
    }
}
