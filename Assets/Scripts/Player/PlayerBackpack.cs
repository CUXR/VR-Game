using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class PlayerBackpack : MonoBehaviour
{
    [Header("Backpack State")]
    public bool isOpen;
    public bool dropdownVisible;

    [Header("Backpack References")]
    public GameObject backpackUI;
    public TMP_Dropdown dropdownUI;
    public GameObject[] backpackSlots;

    void Start()
    {
        isOpen = false;
        dropdownVisible = false;
        backpackUI.SetActive(false);
        dropdownUI.gameObject.SetActive(false);
        dropdownUI.ClearOptions();

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

            if (!isOpen)
            {
                dropdownVisible = false;
            }
        }

        dropdownUI.gameObject.SetActive(dropdownVisible && isOpen);
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
        backpackSlots[slotIndex].GetComponent<Button>().onClick.AddListener(() => ToggleDropdown(item));
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
                backpackSlots[i].GetComponent<Button>().onClick.RemoveAllListeners();
                return true;
            }
        }
        return false;
    }

    void ToggleDropdown(GameObject item)
    {
        dropdownVisible = !dropdownVisible;

        // Update the dropdown options based on the currently selected item
        if (dropdownVisible)
        {
            List<string> actions = item.GetComponent<Collectible>().collectibleActions.Select(action => action.ToString()).ToList();

            dropdownUI.ClearOptions();
            dropdownUI.AddOptions(actions);

            dropdownUI.transform.parent = item.transform.parent; // Set the dropdown's parent to the slot
            dropdownUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70);
        }
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
