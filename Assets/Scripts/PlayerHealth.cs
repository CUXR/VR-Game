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

    public float regenAmount = 50f;

    [Header("Decay Settings")]
    public float defaultDecayRate = 1f;
    public float stealthMultiplier = 2f;
    public float aggressiveMultiplier = 4f;

    [HideInInspector] public float currentHealth;
    [HideInInspector] public float currentDecayRate;

    private PlayerMovement playerMovement;


    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        currentHealth = 100f;
        currentDecayRate = defaultDecayRate;
        SyncSliderHealth();
    }

    void Update()
    {
        switch (playerMovement.movementState)
        {
            case PlayerMovement.MovementState.IDLE:
                currentDecayRate = defaultDecayRate;
                break;
            case PlayerMovement.MovementState.WALK:
            case PlayerMovement.MovementState.CROUCH:
                currentDecayRate = defaultDecayRate * stealthMultiplier;
                break;
            case PlayerMovement.MovementState.SPRINT:
            case PlayerMovement.MovementState.WALLRUN:
            case PlayerMovement.MovementState.AIR:
                currentDecayRate = defaultDecayRate * aggressiveMultiplier;
                break;
        }

        // Apply the multiplier to health decay
        currentHealth = Mathf.Clamp(
            currentHealth - defaultDecayRate * currentDecayRate * Time.deltaTime,
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
    private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Enemy"))
    {
        Debug.Log("Collided with enemy!");

        // Decrease health
        currentHealth = Mathf.Clamp(currentHealth - 20f, minHealth, maxHealth);
        SyncSliderHealth();

        // Add explosion force
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 explosionPosition = collision.transform.position;
            float explosionForce = 10000f;
            float explosionRadius = 2f;

            rb.AddExplosionForce(explosionForce, explosionPosition, explosionRadius);
        }
    }
}

}
