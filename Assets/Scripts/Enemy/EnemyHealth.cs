using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour
{
    public bool isAlive = true;
    public Outline outline;
    public Color outlineColor = Color.red;
    public float outlineWidth = 12.0f;

    void Start()
    {
        outline = gameObject.AddComponent<Outline>();
        outline.enabled = false;
        outline.OutlineColor = outlineColor;
        outline.OutlineWidth = outlineWidth;
    }

    public void Stab()
    {
        isAlive = false;
        outline.enabled = false;
        Destroy(gameObject);
    }
}
