using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

        if (hit.transform.GetComponentInParent<EnemyHealth>() is EnemyHealth enemy
            && enemy.isAlive)
            // && !enemy.GetComponent<EnemyVision>().PlayerVisible()
            // && !enemy.GetComponent<EnemyVision>().PlayerInvestigate())
        {
            enemy.outline.enabled = true;
            currentEnemy = enemy;

            var playerLimb = GetComponent<PlayerLimb>();
            bool canAttack = playerLimb == null || playerLimb.CurrentArmCount > 0;

            if (canAttack && InputController.Instance.GetStabDown())
            {
                float damage;
                if (playerLimb != null)
                {
                    damage = playerLimb.GetAttackDamage();
                    Debug.Log("Damage calculated: " + damage);
                }
                else
                {
                    damage = 10f;
                }

                enemy.Stab(damage);
                enemy.outline.enabled = false;
            }
        }
    }
}
