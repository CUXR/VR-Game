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
    public Outline outline; // outline settings
    
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
        GameObject uiItem = new GameObject(itemName + "_ui");
        uiItem.AddComponent<RectTransform>();
        uiItem.AddComponent<CanvasRenderer>();
        uiItem.AddComponent<Image>().sprite = itemIcon;

        InventorySlot slot = uiItem.AddComponent<InventorySlot>();
        slot.Setup(this);

        return uiItem;
    }

    public virtual void Use()
    {
        if (isSingleUse) { Destroy(gameObject); }
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

    public virtual void Drop(Transform dropLocation)
    {
        transform.position = dropLocation.position;
        transform.SetParent(null);
        gameObject.SetActive(true);
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
                transform.SetParent(playerBackpack.transform);
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(icon);
            }
        }
    }

    public void SetGlow(bool state)
    {
        outline.enabled = state;
    }
}
