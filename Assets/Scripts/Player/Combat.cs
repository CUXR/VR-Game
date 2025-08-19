using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class Combat : MonoBehaviour
{
    private float range = 2;
    public Boolean enemyInRange = false;
    public GameObject enemyObject;
    void Update()
    {
        enemyInRange = false;
        if (Physics.Raycast(Camera.main.gameObject.transform.position,
                Camera.main.gameObject.transform.forward, out RaycastHit hit, range))
        {
            if (hit.collider.gameObject.TryGetComponent(out Rigidbody rb))
            {
                if (hit.rigidbody.gameObject.TryGetComponent(out EnemyHealth enemy))
                {
                    enemyObject = enemy.gameObject;
                    enemyInRange = true;
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        if (enemy.alive == true)
                        {
                            enemy.stab();
                            gameObject.GetComponent<PlayerHealth>().deplete();
                        }
                    }
                }
            }
        }
    }
}

