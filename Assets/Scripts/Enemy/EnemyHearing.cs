using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class EnemyHearing : MonoBehaviour
{
    [Header("References")]
    private EnemyController enemyController;

    [Header("Hearing Settings")]
    public bool heardSound = false;
    public float maxHearingRange;
    public float hearingRange;
    private float loudnessThreshold = 0.05f;
    private float rangeToDraw;

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
        enemyController.investigatePositions.Push(soundPos);
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
        if (maxHearingRange == 0)
        {
            rangeToDraw = 12f;
        }
        else
        {
            rangeToDraw = maxHearingRange;
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, rangeToDraw);
    }
}

