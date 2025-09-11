using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField]
    private float range = 2;
    private EnemyHealth currentEnemy;

    void Update()
    {
        if (
            !Physics.Raycast(
                Camera.main.gameObject.transform.position,
                Camera.main.gameObject.transform.forward,
                out RaycastHit hit,
                range
            )
        )
        {
            if (currentEnemy != null)
            {
                currentEnemy.outline.enabled = false;
                currentEnemy = null;
            }

            return;
        }

        if (hit.collider.gameObject.TryGetComponent(out EnemyHealth enemy) && enemy.isAlive && !enemy.GetComponent<EnemyVision>().PlayerVisible())
        {
            enemy.outline.enabled = true;
            currentEnemy = enemy;

            if (InputController.Instance.GetStabDown())
            {
                enemy.Stab();
                gameObject.GetComponent<PlayerHealth>().DepleteHealthPercentage(0.25f, true);
            }
        }
    }
}
