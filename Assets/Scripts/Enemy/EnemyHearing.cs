using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHearing : MonoBehaviour
{
    private float loudnessThreshold = 0.05f;
    private EnemyController enemy;
    public bool heardSound = false;
    public Vector3 investigatePos;
    public float maxHearingRange;
    public float hearingRange;

    void Start()
    {
        maxHearingRange = GameController.Instance.maxHearingRange;
        hearingRange = maxHearingRange;
    }

    public float GetRange()
    {
        return hearingRange;
    }

    public void SetRange(float range)
    {
        // To allow different thypes of enemies to have different hearing ranges, but
        // they must all be less than the maximum hearing range
        if (range <= maxHearingRange)
        {
            hearingRange = range;
        }
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
            Gizmos.DrawWireSphere(enemy.transform.position, hearingRange);
        }
    }

}
