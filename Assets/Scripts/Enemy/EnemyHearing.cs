using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    private float maxHearingRange;
    private float loudnessThreshold;
    public bool heardSound = false;
    public Vector3 investigatePos;
    //The enemy should know where a noise is originated when a noise is heard
    //The player should be heard from a far distance while running or jumping (since it's really loud)
    //The player should be heard from a medium distance while walking around
    // The player should only be completely silent when moving while crouching
    //(Bonus) Objects that were thrown should create a noise at the place where it landed/collided with another object

    public float GetRange()
    {
        return maxHearingRange;
    }

    public float GetThreshold()
    {
        return loudnessThreshold;
    }

    public void HeardSound(Vector3 soundPos)
    {
        heardSound = true;
        investigatePos = soundPos;
    }

}
