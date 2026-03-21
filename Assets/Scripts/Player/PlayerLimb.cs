using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttachedLimbData
{
    public float batteryUsage = 10f;
    public float timeToSteal = 2.5f;
}

public class PlayerLimb : MonoBehaviour
{
    [Header("Current Limbs")]
    public Dictionary<Limb.LimbSlot, AttachedLimbData> equippedLimbs = new Dictionary<Limb.LimbSlot, AttachedLimbData>();

    [Header("Stats")]
    public float baseWalkSpeed = 8f;
    public float baseSprintSpeed = 12f;

    public float moveSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;

    private readonly List<Limb.LimbSlot> stealPriority = new List<Limb.LimbSlot>
    {
        Limb.LimbSlot.Legs
    };

    void Start()
    {
        InitializeStartingLimbs();
    }

    private void InitializeStartingLimbs()
    {
        equippedLimbs.Clear();

        // Player starts out with no legs

        RecalculateStats();
    }

    public bool IsMissingLimb(Limb.LimbSlot slot)
    {
        return !equippedLimbs.ContainsKey(slot);
    }

    public bool EquipLimb(Limb limbItem)
    {
        if (!IsMissingLimb(limbItem.limbSlot)) return false;

        AttachedLimbData newLimbData = new AttachedLimbData
        {
            batteryUsage = limbItem.batteryUsage,
            timeToSteal = limbItem.timeToSteal
        };

        equippedLimbs.Add(limbItem.limbSlot, newLimbData);
        RecalculateStats();
        
        return true;
    }

    public void RecalculateStats()
    {
        if (equippedLimbs.ContainsKey(Limb.LimbSlot.Legs))
            moveSpeedMultiplier = 1f; 
        else 
            moveSpeedMultiplier = 0.45f;
    }
    
    public Limb.LimbSlot? TryStealLimb(out AttachedLimbData stolenData)
    {
       stolenData = null;
        if (equippedLimbs.Count == 0) return null;

        foreach (Limb.LimbSlot slot in stealPriority)
        {
            if (equippedLimbs.ContainsKey(slot))
            {
                stolenData = equippedLimbs[slot];
                equippedLimbs.Remove(slot);
                RecalculateStats();
                return slot;
            }
        }
        return null;
    }
}
