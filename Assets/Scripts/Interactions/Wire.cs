using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Wire : MonoBehaviour
{
    public enum WireColor
    {
        Yellow,
        Red,
        Blue
    }

    public WireColor color;
    public List<GameObject> activeObj = new List<GameObject>();
}
