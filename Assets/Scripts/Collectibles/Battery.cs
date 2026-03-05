using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battery : Collectible
{
    public float chargeAmount = 30f; // Amount of charge this battery provides

    public override void Use()
    {
        base.Use();
        GameObject.Find("PLAYER").GetComponent<PlayerHealth>().RestoreHealth(chargeAmount);
    }
}
