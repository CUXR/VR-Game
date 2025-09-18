using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class EnemyHearing : MonoBehaviour
{
    private float loudnessThreshold = 0.05f;
    private EnemyController enemy;
    private float rangeToDraw;
    public bool heardSound = false;
    public Vector3 investigatePos;
    public float maxHearingRange;
    public float hearingRange;

    void Start()
    {
        hearingRange = maxHearingRange;
        maxHearingRange = GameController.Instance.maxHearingRange;
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
            if (maxHearingRange == 0)
            {
                rangeToDraw = 12f;
            }
            else
            {
                rangeToDraw = maxHearingRange;
            }
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemy.transform.position, rangeToDraw);
        }
    }

}

