using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Collectible : MonoBehaviour
{
    public string itemName;
    public string itemDescription;
    public Sprite itemIcon;

    public GameObject ToUI()
    {
        GameObject uiItem = new GameObject(itemName);
        uiItem.AddComponent<RectTransform>();
        uiItem.AddComponent<CanvasRenderer>();
        uiItem.AddComponent<Image>().sprite = itemIcon;
        return uiItem;
    }
}
