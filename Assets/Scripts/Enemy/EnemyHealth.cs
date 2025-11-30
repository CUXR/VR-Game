using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    public bool isAlive = true;
    public Outline outline;
    public Color outlineColor = Color.red;
    public float outlineWidth = 12.0f;

    void Start()
    {
        currentHealth = maxHealth;
        outline = gameObject.AddComponent<Outline>();
        outline.enabled = false;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
    }

    public void TakeDamage(float damage)
    {
        if (!isAlive) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        
        if (currentHealth <= 0f)
        {
            isAlive = false;
            outline.enabled = false;
        }
    }

    public void Stab(float damage)
    {
        TakeDamage(damage);
    }
}
