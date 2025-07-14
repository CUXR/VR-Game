using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class Combat : MonoBehaviour
{
    private float range = 1;
    void Update()
    {
        if (Physics.Raycast(Camera.main.gameObject.transform.position,
                Camera.main.gameObject.transform.forward, out RaycastHit hit, range))
        {
            if (hit.collider.gameObject.TryGetComponent(out Rigidbody rb))
            {
                if (hit.rigidbody.gameObject.TryGetComponent(out EnemyHealth enemy))
                {
                    if (Input.GetKeyDown(KeyCode.R))
                    {
                        enemy.stab();
                        gameObject.GetComponent<PlayerHealth>().deplete();
                    }
                }
            }
        }
    }
}
