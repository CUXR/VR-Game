using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField]
    private float range = 2;
    private EnemyHealth currentEnemy; //the enemy the player is looking at

    void Update()
    {
        // if player is not looking at an enemy but previously was, remove that enemy's outline
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

        // if player is looking at an enemy and it is alive, then highlight it
        // only in this case can a player stab the enemy 
        if (hit.transform.GetComponentInParent<EnemyHealth>() is EnemyHealth enemy
            && enemy.isAlive)
            // these commented lines mandates the player to only hurt enemies stealthily
            // && !enemy.GetComponent<EnemyVision>().PlayerVisible()
            // && !enemy.GetComponent<EnemyVision>().PlayerInvestigate())
        {
            enemy.outline.enabled = true;
            currentEnemy = enemy;

            if (InputController.Instance.GetStabDown())
            {
                enemy.Stab(10f);
                enemy.outline.enabled = false;
            }
        }
    }
}
