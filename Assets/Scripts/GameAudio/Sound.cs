using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sound
{
    public readonly Vector3 position;
    public readonly float radius;
    public readonly float loudness;
    public readonly float decayRate;
    public Sound(Vector3 pos, float rad, float loud, float decay)
    {
        position = pos;
        radius = rad;
        loudness = loud;
        decayRate = decay;
    }
}
