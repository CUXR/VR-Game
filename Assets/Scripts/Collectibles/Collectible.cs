using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Collectible : MonoBehaviour, InteractableInterface
{
    public string itemName;
    public string itemDescription;
    public bool isSingleUse;
    public Sprite itemIcon;
    public Actions[] collectibleActions = new Actions[]
    {
        Actions.NONE,
        Actions.INSPECT,
        Actions.REMOVE,
    };

    public enum Actions
    {
        NONE,
        USE,
        EQUIP,
        UNEQUIP,
        INSPECT,
        REMOVE,
    }

    public virtual GameObject ToUIObject()
    {
        //Debug.Log("ToUIObject entered for " + gameObject);
        GameObject uiItem = new GameObject(itemName);
        uiItem.AddComponent<RectTransform>();
        uiItem.AddComponent<CanvasRenderer>();
        uiItem.AddComponent<Image>().sprite = itemIcon;

        //Debug.Log("ToUIObject returned:" + uiItem);
        return uiItem;
    }

    public void CopyCollectibleData(Collectible newCollectible)
    {
        newCollectible.itemName = itemName;
        newCollectible.itemDescription = itemDescription;
        newCollectible.isSingleUse = isSingleUse;
        newCollectible.itemIcon = itemIcon;
        newCollectible.collectibleActions = collectibleActions;
    }

    public virtual void Use()
    {
        // Default implementation for using the collectible
        print($"Using {itemName}");
    }

    public virtual void Equip()
    {
        // Default implementation for equipping the collectible
        print($"Equipping {itemName}");
    }

    public virtual void Unequip()
    {
        // Default implementation for unequipping the collectible
        print($"Unequipping {itemName}");
    }

    public virtual void Inspect()
    {
        // Default implementation for inspecting the collectible
        print($"Inspecting {itemName}: {itemDescription}");
    }

    public static implicit operator Collectible(GameObject v)
    {
        throw new NotImplementedException();
    }

    public void Interact()
    {
        PlayerBackpack playerBackpack = GameObject.FindWithTag("Player").GetComponent<PlayerBackpack>();

        if (playerBackpack != null)
        {
            GameObject icon = ToUIObject();

            bool success = playerBackpack.AddItem(icon, this);

            if (success)
            {
                Destroy(gameObject);
            }
        }
    }
}
