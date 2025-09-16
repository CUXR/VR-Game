using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sound : MonoBehaviour
{
    private Vector3 position;
    private float radius;
    private float decayMultiplier;
    public Sound(Vector3 pos, float rad, float decay)
    {
        position = pos;
        radius = rad;
        decayMultiplier = decay;
    }
}
