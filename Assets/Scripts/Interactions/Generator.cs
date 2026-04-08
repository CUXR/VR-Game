using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Generator : MonoBehaviour
{
    private int numWires = 0;
    public int requiredWires = 3;
    public List<Door> doors = new List<Door>();
    public List<GameObject> markers = new List<GameObject>();

    void Start()
    {
        Assert.IsTrue(markers.Count==requiredWires);
        for (int i = 0; i < doors.Count; i++)
        {
            doors[i].isButtonDoor = true;
        }

        for (int j = 0; j < markers.Count; j++)
        {
            markers[j].SetActive(false);
        }
    }

    void Update()
    {
        
    }

    void OnTriggerEnter(Collider obj)
    {
        if (obj.CompareTag("Wire"))
        {
            numWires++;
            markers[numWires-1].SetActive(true);
            if (numWires == requiredWires)
            {
                for (int i = 0; i < doors.Count; i++)
                {
                    doors[i].isButtonDoor = false;
                }
            }
            Destroy(obj.gameObject);
        }
        
        if (numWires != requiredWires) {
            TutorialController.Instance.ShowText("generator");
        }
        else {TutorialController.Instance.ShowText("generator_fixed");};
    }

    void OnTriggerExit(Collider obj)
    {
        TutorialController.Instance.HideText("generator");
    }
}
