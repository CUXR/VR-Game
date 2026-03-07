using UnityEngine;

public class Wire : Collectible
{
    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Wire wire = uiObject.AddComponent<Wire>();

        uiObject.name = itemName;
        CopyCollectibleData(wire);

        return uiObject;
    }
}
