using UnityEngine;

public class Limb : Collectible
{
    public enum LimbType
    {
        Arm,
        Leg,
        Head,
        Torso,
    }

    [Header("Limb Settings")]
    public bool isEquipped;
    public bool isMechanical;
    public LimbType limbType;

    [Range(0f, 100f)]
    public float integrity;
    public float batteryUsage;

    [Header("Stealing Settings")]
    public float timeToSteal;

    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Limb limb = uiObject.AddComponent<Limb>();

        uiObject.name = itemName;
        limb.limbType = limbType;
        limb.isEquipped = isEquipped;
        limb.timeToSteal = timeToSteal;
        limb.isMechanical = isMechanical;
        limb.integrity = integrity;
        limb.batteryUsage = batteryUsage;

        CopyCollectibleData(limb);

        return uiObject;
    }

    public override void Equip()
    {
        base.Equip();

        isEquipped = true;
    }

    public override void Unequip()
    {
        base.Unequip();

        isEquipped = false;
    }
}
