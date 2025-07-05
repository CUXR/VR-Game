using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Collectible : MonoBehaviour
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;
    public Actions[] collectibleActions;

    public enum Actions
    {
        USE,
        EQUIP,
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
        newCollectible.itemIcon = itemIcon;
        newCollectible.collectibleActions = collectibleActions;
    }
}
