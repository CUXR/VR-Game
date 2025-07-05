using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    public bool isOpen;

    [Header("Backpack References")]
    public GameObject backpackUI;
    public GameObject[] backpackSlots;

    void Start()
    {
        isOpen = false;
        backpackUI.SetActive(false);
        backpackSlots = new GameObject[backpackUI.transform.childCount];

        for (int i = 0; i < backpackUI.transform.childCount; i++)
        {
            backpackSlots[i] = backpackUI.transform.GetChild(i).gameObject;
        }
    }

    void Update()
    {
        if (InputController.Instance.GetBackpackDown())
        {
            isOpen = !isOpen;
            backpackUI.SetActive(isOpen);
        }
    }

    int FindSmallestOpenSlot()
    {
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (backpackSlots[i].transform.childCount == 0)
            {
                return i;
            }
        }
        return -1;
    }

    bool AddItem(GameObject item)
    {
        int slotIndex = FindSmallestOpenSlot();

        if (slotIndex == -1)
        {
            Debug.Log("Backpack is full!");
            return false;
        }

        item.transform.SetParent(backpackSlots[slotIndex].transform);
        backpackSlots[slotIndex].transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        return true;
    }

    bool RemoveItem(GameObject item)
    {
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (backpackSlots[i].transform.childCount > 0 && backpackSlots[i].transform.GetChild(0).gameObject == item)
            {
                Destroy(item);
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out Collectible collectible))
        {
            if (AddItem(collectible.ToUIObject()))
            {
                Destroy(other.gameObject); // Destroy the collectible object after adding it to the backpack
            }
        }
    }
}
