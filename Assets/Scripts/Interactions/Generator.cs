using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Generator : MonoBehaviour
{
    private int numWires = 0;
    public int requiredWires = 3;
    public Door door;
    public Door left;
    public Door right;
    public List<GameObject> markers = new List<GameObject>();

    void Start()
    {
        Assert.IsTrue(markers.Count==requiredWires);
        door.isButtonDoor = true;
        left.isButtonDoor = true;
        right.isButtonDoor = true;
        for (int i = 0; i < markers.Count; i++)
        {
            markers[i].SetActive(false);
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
                door.isButtonDoor = false;
                right.isButtonDoor = false;
                left.isButtonDoor = false;
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
