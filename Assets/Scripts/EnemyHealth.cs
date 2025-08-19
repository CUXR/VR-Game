using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public GameObject player;
    public bool alive = true;
    private Combat combat;
    private Outline outline;

    void Start()
    {
        combat = player.GetComponent<Combat>();
        outline = gameObject.AddComponent<Outline>();
        outline.enabled = false;
        outline.OutlineColor = Color.red;
        outline.OutlineWidth = 12.0f;
    }

    public void stab()
    {
        alive = false;
        Debug.Log("Enemy killed");
    }

    void Update()
    {
        if (combat.enemyInRange && combat.enemyObject == gameObject)
        {
            outline.enabled = true;
        }
        else
        {
            outline.enabled = false;
        }
    }
}

