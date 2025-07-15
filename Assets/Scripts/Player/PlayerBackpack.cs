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
    public bool isInspecting;

    [Header("Backpack References")]
    public GameObject backpackUI;
    public TMP_Dropdown dropdownUI;
    public GameObject[] backpackSlots;

    [Header("Inspect References")]
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;

    [HideInInspector] public GameObject selectedItem;


    void Start()
    {
        isOpen = false;
        dropdownVisible = false;
        isInspecting = false;

        backpackUI.SetActive(false);
        dropdownUI.gameObject.SetActive(false);
        dropdownUI.ClearOptions();

        itemNameText.gameObject.SetActive(false);
        itemDescriptionText.gameObject.SetActive(false);

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

        itemNameText.gameObject.SetActive(isInspecting);
        itemDescriptionText.gameObject.SetActive(isInspecting);
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
                dropdownUI.transform.parent = backpackUI.transform; // Reset the dropdown's parent to the backpack UI
                selectedItem = null;
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
            selectedItem = item;

            List<string> actions = item.GetComponent<Collectible>().collectibleActions.Select(action => action.ToString()).ToList();

            dropdownUI.ClearOptions();
            dropdownUI.AddOptions(actions);

            dropdownUI.transform.parent = item.transform.parent; // Set the dropdown's parent to the slot
            dropdownUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70);

            dropdownUI.onValueChanged.AddListener(delegate {
                Collectible.Actions selectedAction = (Collectible.Actions)System.Enum.Parse(typeof(Collectible.Actions), dropdownUI.options[dropdownUI.value].text);
                switch (selectedAction)
                {
                    case Collectible.Actions.USE:
                        selectedItem.GetComponent<Collectible>().Use();

                        if (selectedItem.GetComponent<Collectible>().isSingleUse)
                        {
                            RemoveItem(selectedItem);
                        }

                        dropdownVisible = false;
                        isInspecting = false;

                        break;

                    // TODO: Add cases for equipping, unequipping, and inspecting items

                    case Collectible.Actions.INSPECT:
                        isInspecting = true;

                        itemNameText.text = selectedItem.GetComponent<Collectible>().itemName;
                        itemDescriptionText.text = selectedItem.GetComponent<Collectible>().itemDescription;
                        break;

                    case Collectible.Actions.REMOVE:
                        if (RemoveItem(selectedItem))
                        {
                            dropdownVisible = false; // Close the dropdown after removing the item
                            isInspecting = false; // Close the inspection view
                        }
                        break;

                    default:
                        Debug.Log("No action selected for item: " + selectedItem.name);
                        break;
                }
            });
        }

        else
        {
            selectedItem = null;
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
