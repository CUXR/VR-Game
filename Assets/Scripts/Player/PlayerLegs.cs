using UnityEngine;

[System.Serializable]
public class AttachedLegsData
{
    public float timeToSteal = 2.5f; 
}

public class PlayerLegs : MonoBehaviour
{
    [Header("Current Status")]
    public bool hasLegs = true;
    public AttachedLegsData currentLegsData;

    [Header("Movement Stats")]
    public float baseWalkSpeed = 8f;
    public float baseSprintSpeed = 12f;
    public float moveSpeedMultiplier = 1f;

    void Start()
    {
        InitializeStartingLegs();
    }

    private void InitializeStartingLegs()
    {
        hasLegs = false;
        RecalculateStats();
    }

    public bool IsMissingLegs()
    {
        return !hasLegs;
    }

    public bool EquipLegs(Legs legsItem)
    {
        if (!IsMissingLegs()) return false;

        currentLegsData = new AttachedLegsData
        {
            timeToSteal = legsItem.timeToSteal
        };

        hasLegs = true;
        RecalculateStats();
        
        return true;
    }

    public void RecalculateStats()
    {
        if (hasLegs)
        {
            moveSpeedMultiplier = 1f;
        }
        else
        {
            moveSpeedMultiplier = 0.45f;
        }
    }

    public bool TryStealLegs(out AttachedLegsData stolenData)
    {
        stolenData = null;
        
        if (!hasLegs) return false; 

        stolenData = currentLegsData;
        
        currentLegsData = null;
        hasLegs = false;
        
        RecalculateStats();
        
        return true; 
    }
}