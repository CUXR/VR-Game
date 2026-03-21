using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Collectible itemReference;

    public void Setup(Collectible item)
    {
        itemReference = item;
    }

    public void OnUse()
    {
        if (itemReference != null)
        {
            itemReference.Use(); 

            if (itemReference == null || itemReference.isSingleUse)
            {
                Destroy(gameObject);
            }
        }
    }

    public void OnDrop(Transform dropLocation)
    {
        if (itemReference != null)
        {
            itemReference.Drop(dropLocation);
            Destroy(gameObject);
        }
    }
}