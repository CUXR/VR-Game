using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private float range = 2;
    public bool enemyInRange = false;
    public GameObject currentEnemy;

    void Update()
    {
        if (!Physics.Raycast(Camera.main.gameObject.transform.position,
                Camera.main.gameObject.transform.forward, out RaycastHit hit, range)) return;


        if (hit.collider.gameObject.TryGetComponent(out Rigidbody rb) &&
            rb.gameObject.TryGetComponent(out EnemyHealth enemy)
            && enemy.isAlive)
        {

            currentEnemy = enemy.gameObject;
            enemyInRange = true;
            enemy.outline.enabled = true;

            if (InputController.Instance.GetStabDown())
            {
                enemy.Stab();
                gameObject.GetComponent<PlayerHealth>().Deplete();
                enemyInRange = false;
            }
        }

        else
        {
            currentEnemy.GetComponent<EnemyHealth>().outline.enabled = false;
        }
    }
}

