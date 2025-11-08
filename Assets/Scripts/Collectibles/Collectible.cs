using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Collectible : MonoBehaviour, Interactable
{
    public string itemName;
    public string itemDescription;
    public bool isSingleUse;
    public Sprite itemIcon;
    public Actions[] collectibleActions;

    public enum Actions
    {
        NONE,
        USE,
        EQUIP,
        UNEQUIP,
        INSPECT,
        REMOVE
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
        Debug.Log($"Using {itemName}");
    }

    public virtual void Inspect()
    {
        // Default implementation for inspecting the collectible
        Debug.Log($"Inspecting {itemName}: {itemDescription}");
    }

    public void Interact()
    {
        throw new System.NotImplementedException();
    }
}
