using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    private Boolean alive = true;
    public void stab()
    {
        alive = false;
        Debug.Log(alive);
    }
}
