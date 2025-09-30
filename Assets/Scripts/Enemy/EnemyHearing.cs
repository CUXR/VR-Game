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
    public float maxHearingRange;
    public float hearingRange;
    private float loudnessThreshold = 0.05f;
    private float rangeToDraw;
    bool heardSound;

    void Start()
    {
        hearingRange = maxHearingRange;
        enemyController = GetComponent<EnemyController>();
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

    public void AddSoundPosition(Vector3 soundPos)
    {
        enemyController.AddInvestigatePosition(soundPos);
        StartCoroutine(HeardSoundCoroutine());

        IEnumerator HeardSoundCoroutine()
        {
            heardSound = true;
            yield return new WaitForSeconds(Time.deltaTime);
            heardSound = false;
        }
    }

    public bool HeardSound()
    {
        return heardSound;        
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

