using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public GameObject player;
    public bool isAlive = true;
    public Outline outline;

    void Start()
    {
        outline = gameObject.AddComponent<Outline>();
        outline.enabled = false;
        outline.OutlineColor = Color.red;
        outline.OutlineWidth = 12.0f;
    }

    public void Stab()
    {
        isAlive = false;
        Debug.Log("Enemy killed");
    }
}

