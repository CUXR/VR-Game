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
    public bool isOpen; //refers to whether the backpack panel is open
    public bool dropdownVisible; //refers to whether a slot has been clicked
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
    public GameObject selectedSlot; // ui display for the item
    private PlayerLimb playerLimb;

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
        playerLimb = GetComponent<PlayerLimb>();

        // initialize backpack slots
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

        UpdateBackpackElementVisibility(); // keeps child ui elements in sync with panel 
    }

    private void UpdateBackpackElementVisibility()
    {
        dropdownUI.gameObject.SetActive(dropdownVisible && isOpen);

        // if i am inspecting an item, i can see the name and description
        // might want to delete at a later point if this feels unnecessary
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

        //put the item in the slot and center the item
        item.transform.SetParent(backpackSlots[slotIndex].transform);
        backpackSlots[slotIndex]
            .transform.GetChild(0)
            .GetComponent<RectTransform>()
            .anchoredPosition = Vector2.zero;
        
        //makes the slot clickable where clicking triggers the dropdown menu
        backpackSlots[slotIndex]
            .GetComponent<Button>()
            .onClick.AddListener(() => ToggleDropdown(item));

        //saves into array
        items[slotIndex] = obj.itemName;
        return true;
    }

    bool RemoveItem(GameObject item)
    {
        // iterates through the slots looking for the item
        for (int i = 0; i < backpackSlots.Length; i++)
        {
            if (
                backpackSlots[i].transform.childCount > 0
                && backpackSlots[i].transform.GetChild(0).gameObject == item
            )
            {
                // removes button clicks, hides dropdown, and turns off inspection panel
                backpackSlots[i].GetComponent<Button>().onClick.RemoveAllListeners();
                dropdownUI.onValueChanged.RemoveAllListeners(); 
                dropdownUI.transform.SetParent(backpackUI.transform); 
                dropdownUI.ClearOptions();
                dropdownVisible = false;

                isInspecting = false;
                selectedSlot = null;

                Destroy(item);
                items[i] = "";
                return true;
            }
        }
        return false;
    }

    // currently not used
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
        // if clicking a selected item, toggles off; if clicking new item, toggles on
        if (selectedSlot == item || selectedSlot == null)
        {
            dropdownVisible = !dropdownVisible;
        }

        // update the dropdown options based on the currently selected item
        if (dropdownVisible)
        {
            selectedSlot = item;

            Collectible itemData = selectedSlot.GetComponent<InventorySlot>().itemReference; // item itself

            List<string> actions = itemData
                .collectibleActions.Select(action => action.ToString())
                .ToList(); // actions available can be set from the editor

            // refresh with new list
            dropdownUI.ClearOptions();
            dropdownUI.AddOptions(actions);

            // to make the dropdown appear slightly below the slot
            dropdownUI.transform.SetParent(selectedSlot.transform.parent);
            dropdownUI.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -70);

            // so that the player doesn't trigger actions from a previously selected item
            dropdownUI.onValueChanged.RemoveAllListeners();    

            // add new listener that triggers when player picks an option
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
                            itemData.Use();

                            if (itemData.isSingleUse)
                            {
                                RemoveItem(selectedSlot);
                            }

                            isInspecting = false;
                            break;

                        case Collectible.Actions.EQUIP:
                            //checks if item is specifically a limb 
                            Limb limbToEquip = itemData.GetComponent<Limb>();
                            if (limbToEquip != null)
                            {   
                                //attempt to equip; if successful, removes from bag
                                if (playerLimb != null && playerLimb.EquipLimb(limbToEquip))
                                {
                                    RemoveItem(selectedSlot);
                                }
                                else
                                {
                                    Debug.Log("Cannot equip: You already have a limb of that type");
                                }
                            }
                            else
                            {
                                //for other non-limb items
                                itemData.Equip();
                            }

                            //clean up after equipping
                            isInspecting = false;
                            dropdownVisible = false;
                            break;

                        case Collectible.Actions.INSPECT:
                            InspectItem();
                            break;

                        case Collectible.Actions.REMOVE:
                            // for now, deletes the item entirely (does not drop it)
                            // hiddenItem.Drop(GameObject.FindWithTag("Player").transform);
                            RemoveItem(selectedSlot);
                            break;

                        default:
                            Debug.Log("No action selected for item: " + selectedSlot.name);
                            break;
                    }
                }
            );
        }
        else
        {
            selectedSlot = null;
        }
    }

    // to inspect an item, show the name and description
    // may want to remove in the future
    private void InspectItem()
    {
        isInspecting = true;

        Collectible itemData = selectedSlot.GetComponent<InventorySlot>().itemReference;

        itemNameText.text = itemData.itemName;
        itemDescriptionText.text = itemData.itemDescription;
    }
}
