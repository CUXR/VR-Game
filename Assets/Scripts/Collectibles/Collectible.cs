using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Collectible : MonoBehaviour
{
    public string itemName;
    public string itemDescription;
    public bool isSingleUse;
    public Sprite itemIcon;
    public Actions[] collectibleActions = new Actions[] { Actions.NONE , Actions.INSPECT, Actions.REMOVE };

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
        GameObject uiItem = new GameObject(itemName);
        uiItem.AddComponent<RectTransform>();
        uiItem.AddComponent<CanvasRenderer>();
        uiItem.AddComponent<Image>().sprite = itemIcon;

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
}
