using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Battery : Collectible
{
    public float chargeAmount = 30f; // Amount of charge this battery provides

    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Battery battery = uiObject.AddComponent<Battery>();

        uiObject.name = itemName;
        battery.chargeAmount = chargeAmount;
        CopyCollectibleData(battery);

        return uiObject;
    }
}
