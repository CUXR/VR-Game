using System.Collections.Generic;
using UnityEngine;

public class Legs : Collectible
{
    [Header("Settings")]
    public bool isEquipped;

    [Header("Stealing Settings")]
    public float timeToSteal;

    public void CopyLimbDataTo(Legs target)
    {
        target.isEquipped = isEquipped;
        target.timeToSteal = timeToSteal;
        
        CopyCollectibleData(target);
    }

    public override GameObject ToUIObject()
    {
        GameObject uiObject = base.ToUIObject();
        Legs legs = uiObject.AddComponent<Legs>();
        uiObject.name = itemName;

        CopyLimbDataTo(legs);

        return uiObject;
    }

    public void InteractWith(GameObject playerObj)
    {
        if (playerObj == null) return; 
        PlayerLegs playerLegs = playerObj.GetComponent<PlayerLegs>();
        if (playerLegs == null) return; 

        if (playerLegs.IsMissingLegs())
        {
            ReattachLegs(playerLegs);
        }
        else
        {
            AddToInventory(playerObj);
        }
    }

    private void ReattachLegs(PlayerLegs playerLegs)
    {
        bool successfullyEquipped = playerLegs.EquipLegs(this);
        
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