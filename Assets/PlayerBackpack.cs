using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    public bool isOpen;
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
}
