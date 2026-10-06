using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Wire : MonoBehaviour
{
    // public enum WireColor
    // {
    //     Yellow,
    //     Red,
    //     Blue
    // }

    // public WireColor color;
    public GameObject[] activeObj;
    public GameObject[] difMatObj;
    public Material[] difMats;

    void Start()
    {
        Debug.Assert(difMatObj.Length == difMats.Length, "difMatObj and activeObj must be the same length");
    }
    
    public void setObjsActive()
    {
        for(int i = 0; i < activeObj.Length; i++)
        {
            activeObj[i].SetActive(true);
        }
    }

    public void changeObjMats()
    {
        for(int i = 0; i < difMatObj.Length; i++)
        {
            difMatObj[i].GetComponent<MeshRenderer>().material = difMats[i];
        }
    }

}