using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sound : MonoBehaviour
{
    public readonly Vector3 position;
    public readonly float radius;
    public readonly float decayMultiplier;
    public Sound(Vector3 pos, float rad, float decay)
    {
        position = pos;
        radius = rad;
        decayMultiplier = decay;
    }
}
