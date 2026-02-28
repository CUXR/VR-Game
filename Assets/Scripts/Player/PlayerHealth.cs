using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("References")]
    public Slider healthSlider;
    public TMPro.TMP_Text limbStatusText;
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
    public float crawlMultiplier = 0.1f;

    [HideInInspector]
    public float currentHealth;

    [HideInInspector]
    public float currentDecayRate;

    private PlayerMovement playerMovement;
    private PlayerLimb playerLimb;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerLimb = GetComponent<PlayerLimb>();

        if (playerLimb == null)
            Debug.LogWarning("PlayerLimb reference not found for PlayerHealth; limb UI won't update.", this);
        if (limbStatusText == null)
            Debug.LogWarning("`limbStatusText` is not assigned in the inspector for PlayerHealth.", this);

        currentHealth = 100f;
        currentDecayRate = defaultDecayRate;
        SyncSliderHealth();
        UpdateLimbStatus();
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
            case PlayerMovement.MovementState.CRAWL:
                currentDecayRate = crawlMultiplier;
                break;
        }
        Debug.Log(defaultDecayRate * currentDecayRate * Time.deltaTime);
        // Apply the multiplier to health decay
        currentHealth = Mathf.Clamp(
            currentHealth - defaultDecayRate * currentDecayRate * Time.deltaTime,
            minHealth,
            maxHealth
        );

        SyncSliderHealth();
        UpdateLimbStatus();
    }

    public void UpdateLimbStatus()
    {
        int num_arms = playerLimb.CurrentArmCount;
        int num_legs = playerLimb.CurrentLegCount;

        limbStatusText.text = $"Arms: {num_arms} | Legs: {num_legs}";
    }
    public void UpdateLimbStatus(int numArms, int numLegs)
    {
        limbStatusText.text = $"Arms: {numArms} | Legs: {numLegs}";
    }

    void SyncSliderHealth()
    {
        healthSlider.value = currentHealth;

        // Set the fill color based on current health
        healthSlider.fillRect.GetComponent<Image>().color =
            currentHealth <= dangerHealth ? dangerHealthColor : defaultHealthColor;
    }

    public bool isPlayerDead()
    {
        return currentHealth <= minHealth;
    }

    public bool isPlayerInDanger()
    {
        return currentHealth <= dangerHealth && currentHealth > minHealth;
    }

    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, minHealth, maxHealth);
        SyncSliderHealth();
    }

    public void DepleteHealthFixed(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, minHealth, maxHealth);
    }

    public void DepleteHealthPercentage(float percentage, bool isMaxHealthBased)
    {
        if (isMaxHealthBased)
            currentHealth = Mathf.Clamp(
                currentHealth - (maxHealth * percentage),
                minHealth,
                maxHealth
            );
        else
            currentHealth = Mathf.Clamp(
                currentHealth - (currentHealth * percentage),
                minHealth,
                maxHealth
            );
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Handle collision with enemy
            DepleteHealthFixed(10f);
        }
    }
}
