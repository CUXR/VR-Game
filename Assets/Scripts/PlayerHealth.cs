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
        currentHealth = Mathf.Clamp(currentHealth - defaultDecayRate * Time.deltaTime, minHealth, maxHealth);
        SyncSliderHealth();
    }

    void SyncSliderHealth()
    {
        healthSlider.value = currentHealth;

        // Set the fill color based on current health
        healthSlider.fillRect.GetComponent<Image>().color = 
            currentHealth <= dangerHealth ? dangerHealthColor : defaultHealthColor;
    }
}
