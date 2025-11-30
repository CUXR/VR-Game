using UnityEngine;

public class Limb : Collectible
{
    public enum LimbType
    {
        Arm,
        Leg,
    }

    [Header("Limb Settings")]
    public bool isEquipped;
    public LimbType limbType;

    [Range(0f, 100f)]
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
        limb.batteryUsage = batteryUsage;

        CopyCollectibleData(limb);

        return uiObject;
    }

    public void InteractWith()
    {
        GameObject playerObj = GameObject.Find("PLAYER");
        if (playerObj == null) return;

        PlayerLimb playerLimb = playerObj.GetComponent<PlayerLimb>();
        if (playerLimb == null) return;

        bool isMissingLimbType = false;

        if (limbType == LimbType.Arm)
            isMissingLimbType = playerLimb.CurrentArmCount < 2;
        else if (limbType == LimbType.Leg)
            isMissingLimbType = playerLimb.CurrentLegCount < 2;

        if (isMissingLimbType)
        {
            ReattachLimb(playerLimb);
        }
        else
        {
            AddToInventory();
        }
    }

    private void ReattachLimb(PlayerLimb playerLimb)
    {
        GameObject limbObj = new GameObject();
        limbObj.hideFlags = HideFlags.HideAndDontSave;
        Limb newLimb = limbObj.AddComponent<Limb>();
        newLimb.limbType = limbType;
        newLimb.isEquipped = true;
        newLimb.batteryUsage = batteryUsage;
        newLimb.timeToSteal = timeToSteal;
        newLimb.itemName = itemName;
        newLimb.itemDescription = itemDescription;
        newLimb.isSingleUse = isSingleUse;
        newLimb.itemIcon = itemIcon;

        playerLimb.equippedLimbs.Add(newLimb);
        playerLimb.RecalculateStats();

        Destroy(gameObject);
    }

    private void AddToInventory()
    {
        GameObject playerObj = GameObject.Find("PLAYER");
        if (playerObj == null) {
            Debug.Log("couldn't find the player object");
            return;
        };

        PlayerBackpack backpack = playerObj.GetComponent<PlayerBackpack>();
        if (backpack == null) {
            Debug.Log("couldn't find the backpack object");
            return;
        };

        GameObject uiItem = ToUIObject();
        if (backpack.AddItem(uiItem))
        {
            Destroy(gameObject);
        }
        else
        {
            Destroy(uiItem);
        }
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