using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public bool alive = true;
    public void stab()
    {
        alive = false;
        Debug.Log("Enemy killed");
    }
}

