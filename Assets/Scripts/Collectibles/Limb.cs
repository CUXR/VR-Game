using UnityEngine;

public class Limb : Collectible
{
    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Limb limb = uiObject.AddComponent<Limb>();

        uiObject.name = itemName;
        CopyCollectibleData(limb);

        return uiObject;
    }

    public override void Use()
    {
        base.Use();
        print($"Limb {itemName} used.");
    }
}
