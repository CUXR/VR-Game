using UnityEngine;

public class Limb : Collectible
{
    public enum LimbSlot
    {
        LeftArm, 
        RightArm, 
        LeftLeg, 
        RightLeg
    }

    [Header("Limb Settings")]
    public bool isEquipped;
    public LimbSlot limbSlot;

    [Range(0f, 100f)]
    public float batteryUsage;

    [Header("Stealing Settings")]
    public float timeToSteal;

    public void CopyLimbDataTo(Limb targetLimb)
    {
        targetLimb.limbSlot = limbSlot;
        targetLimb.isEquipped = isEquipped;
        targetLimb.batteryUsage = batteryUsage;
        targetLimb.timeToSteal = timeToSteal;
        
        CopyCollectibleData(targetLimb);
    }

    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Limb limb = uiObject.AddComponent<Limb>();
        uiObject.name = itemName;

        CopyLimbDataTo(limb);

        return uiObject;
    }

    public void InteractWith(GameObject playerObj)
    {
        if (playerObj == null) return; 
        PlayerLimb playerLimb = playerObj.GetComponent<PlayerLimb>();
        if (playerLimb == null) return; 

        if (playerLimb.IsMissingLimb(limbSlot))
        {
            ReattachLimb(playerLimb);
        }
        else
        {
            AddToInventory(playerObj);
        }
    }

    private void ReattachLimb(PlayerLimb playerLimb)
    {
        bool successfullyEquipped = playerLimb.EquipLimb(this);
        
        if (successfullyEquipped)
        {
            Destroy(gameObject);
        }
    }

    private void AddToInventory(GameObject playerObj)
    {
        PlayerBackpack backpack = playerObj.GetComponent<PlayerBackpack>();
        if (backpack == null) 
        {
            Debug.LogWarning("Couldn't find backpack on player");
            return;
        }

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