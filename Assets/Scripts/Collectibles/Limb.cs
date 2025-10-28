using UnityEngine;

public class Limb : Collectible
{
    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Battery battery = uiObject.AddComponent<Battery>();

        uiObject.name = itemName;
        CopyCollectibleData(battery);

        return uiObject;
    }

    public override void Use()
    {
        base.Use();
        print($"Limb {itemName} used.");
    }
}
