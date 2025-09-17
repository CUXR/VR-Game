using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    private float maxHearingRange = 5f;
    private float loudnessThreshold;
    private EnemyController enemy;
    private bool investigating = false;
    public bool heardSound = false;
    public Vector3 investigatePos;
    //The player should be heard from a far distance while running or jumping (since it's really loud)
    //The player should be heard from a medium distance while walking around
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
        investigatePos = soundPos;
        heardSound = true;
        investigating = true;
    }

    void FixedUpdate()
    {
        if (heardSound)
        {
            heardSound = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (enemy == null)
        {
            enemy = GetComponent<EnemyController>();
        }
        if (enemy != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy.transform.position, maxHearingRange);
        }
    }

}
