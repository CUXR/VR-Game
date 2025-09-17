using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    private float maxHearingRange = 5f;
    private float loudnessThreshold = 0.05f;
    private EnemyController enemy;
    public bool heardSound = false;
    public Vector3 investigatePos;

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
