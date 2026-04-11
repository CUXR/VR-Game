using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Generator : MonoBehaviour
{
    private int numWires = 0;
    public int requiredWires = 3;
    public List<Door> doors = new List<Door>();
    public List<GameObject> wires = new List<GameObject>();
    public GameObject button;
    public Material buttonMat;
    public List<GameObject> cylinders = new List<GameObject>();
    public List<Material> cylinderMats = new List<Material>();

    void Start()
    {
        Assert.IsTrue(wires.Count==requiredWires);
        for (int i = 0; i < doors.Count; i++)
        {
            doors[i].isButtonDoor = true;
        }

        for (int j = 0; j < wires.Count; j++)
        {
            wires[j].SetActive(false);
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
            wires[numWires-1].SetActive(true);
            cylinders[numWires-1].GetComponent<MeshRenderer>().material = cylinderMats[numWires-1];
            if (numWires == requiredWires)
            {
                button.GetComponent<MeshRenderer>().material = buttonMat;
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
        TutorialController.Instance.HideText("generator_fixed");
    }
}
