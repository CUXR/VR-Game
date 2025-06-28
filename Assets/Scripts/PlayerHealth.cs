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

    void Start()
    {
        currentHealth = 100f;
        SyncSliderHealth();
    }

    void Update()
    {
        // Clamp health between max health and min health
        currentHealth = Mathf.Clamp(currentHealth - defaultDecayRate * Time.deltaTime, dangerHealth, maxHealth);
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
