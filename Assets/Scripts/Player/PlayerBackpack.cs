using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBackpack : MonoBehaviour
{
    [Header("Backpack State")]
    public int numSlots;
    public bool isOpen;
    public bool dropdownVisible;
    public bool isInspecting;

    [Header("Backpack References")]
    public GameObject slotPrefab;
    public GameObject backpackPanel;
    public GameObject backpackUI;
    public TMP_Dropdown dropdownUI;
    public GameObject[] backpackSlots;
    [SerializeField] public static String[] items;

    [Header("Inspect References")]
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;

    [HideInInspector]
    public GameObject selectedItem;

    void Start()
    {
        isOpen = false;
        dropdownVisible = false;
        isInspecting = false;

        backpackPanel.SetActive(false);
        backpackUI.SetActive(false);
        dropdownUI.gameObject.SetActive(false);
        dropdownUI.ClearOptions();

        itemNameText.gameObject.SetActive(false);
        itemDescriptionText.gameObject.SetActive(false);

        backpackSlots = new GameObject[numSlots];
        items = new String[numSlots];

        // Initialize backpack slots
        for (int i = 0; i < numSlots; i++)
        {
            GameObject slot = Instantiate(slotPrefab, backpackUI.transform);
            backpackSlots[i] = slot;
        }
    }

    void Update()
    {
        if (InputController.Instance.GetBackpackDown())
        {
            ToggleBackpack();
        }

        UpdateBackpackElementVisibility();
    }

    private void UpdateBackpackElementVisibility()
    {
        dropdownUI.gameObject.SetActive(dropdownVisible && isOpen);

        itemNameText.gameObject.SetActive(isInspecting);
        itemDescriptionText.gameObject.SetActive(isInspecting);
    }

    private void ToggleBackpack()
    {
        isOpen = !isOpen;
        Time.timeScale = isOpen ? 0f : 1f;
        backpackPanel.SetActive(isOpen);
        backpackUI.SetActive(isOpen);

        if (!isOpen)
        {
            dropdownVisible = false;
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

    public bool AddItem(GameObject item, Collectible obj)
    {
        int slotIndex = FindSmallestOpenSlot();

        if (slotIndex == -1)
        {
            Debug.Log("Backpack is full!");
            return false;
        }

        item.transform.SetParent(backpackSlots[slotIndex].transform);
        backpackSlots[slotIndex]
            .GetComponent<Button>()
            .onClick.AddListener(() => ToggleDropdown(item));
        backpackSlots[slotIndex]
            .transform.GetChild(0)
            .GetComponent<RectTransform>()
            .anchoredPosition = Vector2.zero;

        items[slotIndex] = obj.itemName;
        return true;
    }

    public bool AddItem(GameObject item)
    {
        int slotIndex = FindSmallestOpenSlot();

        if (slotIndex == -1)
        {
            Debug.Log("Backpack is full!");
            return false;
        }

        item.transform.SetParent(backpackSlots[slotIndex].transform);
        backpackSlots[slotIndex]
            .GetComponent<Button>()
            .onClick.AddListener(() => ToggleDropdown(item));
        backpackSlots[slotIndex]
            .transform.GetChild(0)
            .GetComponent<RectTransform>()
            .anchoredPosition = Vector2.zero;

        return true;
    }

    bool RemoveItem(GameObject item)
    {
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (
                backpackSlots[i].transform.childCount > 0
                && backpackSlots[i].transform.GetChild(0).gameObject == item
            )
            {
                backpackSlots[i].GetComponent<Button>().onClick.RemoveAllListeners();

                dropdownUI.transform.SetParent(backpackUI.transform); // Reset the dropdown's parent to the backpack UI
                dropdownUI.ClearOptions();
                dropdownVisible = false;

                isInspecting = false;
                selectedItem = null;

                Destroy(item);
                items[i] = "";
                return true;
            }
        }
        return false;
    }

    void ExtendBackpack(int newSize)
    {
        if (newSize <= numSlots)
            return;

        GameObject[] newSlots = new GameObject[newSize];
        String[] newItems = new String[newSize];
        for (int i = 0; i < newSize; i++)
        {
            if (i < numSlots)
            {
                newSlots[i] = backpackSlots[i];
            }
            else
            {
                newSlots[i] = Instantiate(slotPrefab, backpackUI.transform);
                newSlots[i].GetComponent<Button>().onClick.AddListener(() => ToggleDropdown(null));
            }
            newItems[i] = items[i];
        }

        backpackSlots = newSlots;
        items = newItems;
        numSlots = newSize;
    }

    void ToggleDropdown(GameObject item)
    {
        if (selectedItem == item || selectedItem == null)
        {
            dropdownVisible = !dropdownVisible;
        }

        // Update the dropdown options based on the currently selected item
        if (dropdownVisible)
        {
            selectedItem = item;

            List<string> actions = selectedItem
                .GetComponent<Collectible>()
                .collectibleActions.Select(action => action.ToString())
                .ToList();

            dropdownUI.ClearOptions();
            dropdownUI.AddOptions(actions);

            dropdownUI.transform.SetParent(selectedItem.transform.parent);
            dropdownUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70);

            dropdownUI.onValueChanged.AddListener(
                delegate
                {
                    Collectible.Actions selectedAction = (Collectible.Actions)
                        System.Enum.Parse(
                            typeof(Collectible.Actions),
                            dropdownUI.options[dropdownUI.value].text
                        );
                    switch (selectedAction)
                    {
                        case Collectible.Actions.NONE:
                            isInspecting = false;
                            break;

                        case Collectible.Actions.USE:
                            print(selectedItem.transform.parent);
                            print(selectedItem);
                            selectedItem.GetComponent<Collectible>().Use();
                            print(selectedItem);

                            if (selectedItem.GetComponent<Collectible>().isSingleUse)
                            {
                                RemoveItem(selectedItem);
                            }

                            isInspecting = false;

                            break;

                        // TODO: Add cases for equipping, unequipping, and inspecting items

                        case Collectible.Actions.INSPECT:
                            InspectItem();
                            break;

                        case Collectible.Actions.REMOVE:
                            RemoveItem(selectedItem);
                            break;

                        default:
                            Debug.Log("No action selected for item: " + selectedItem.name);
                            break;
                    }
                }
            );
        }
        else
        {
            selectedItem = null;
        }
    }

    private void InspectItem()
    {
        isInspecting = true;
        Debug.Log("I am currently inspecting: " + selectedItem.name);
        Debug.Log("Pulled Description: " + selectedItem.GetComponent<Collectible>().itemDescription);

        itemNameText.text = selectedItem.GetComponent<Collectible>().itemName;
        itemDescriptionText.text = selectedItem.GetComponent<Collectible>().itemDescription;
    }
}
