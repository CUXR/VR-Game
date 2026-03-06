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
    public float baseAttackDamage = 100f; // 2 arms = one-shot, 1 arm = two-shot 

    public float moveSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;

    public int CurrentArmCount { get; private set; }
    public int CurrentLegCount { get; private set; }

    private readonly List<Limb.LimbSlot> stealPriority = new List<Limb.LimbSlot>
    {
        Limb.LimbSlot.LeftArm,  // Most expendable
        Limb.LimbSlot.RightArm,
        Limb.LimbSlot.RightLeg,
        Limb.LimbSlot.LeftLeg   // Least expendable
    };

    void Start()
    {
        InitializeStartingLimbs();
    }

    private void InitializeStartingLimbs()
    {
        equippedLimbs.Clear();

        // Player starts out missing their left leg
        equippedLimbs.Add(Limb.LimbSlot.LeftArm, new AttachedLimbData());
        equippedLimbs.Add(Limb.LimbSlot.RightArm, new AttachedLimbData());
        equippedLimbs.Add(Limb.LimbSlot.RightLeg, new AttachedLimbData());

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
        CurrentArmCount = 0;
        CurrentLegCount = 0;

        if (equippedLimbs.ContainsKey(Limb.LimbSlot.LeftArm)) CurrentArmCount++;
        if (equippedLimbs.ContainsKey(Limb.LimbSlot.RightArm)) CurrentArmCount++;
        
        if (equippedLimbs.ContainsKey(Limb.LimbSlot.LeftLeg)) CurrentLegCount++;
        if (equippedLimbs.ContainsKey(Limb.LimbSlot.RightLeg)) CurrentLegCount++;

        if (CurrentLegCount >= 2)
            moveSpeedMultiplier = 1f;
        else if (CurrentLegCount == 1)
            moveSpeedMultiplier = 0.45f;
        else
            moveSpeedMultiplier = 0f;

        if (CurrentArmCount >= 2)
            attackDamageMultiplier = 1f;
        else if (CurrentArmCount == 1)
            attackDamageMultiplier = 0.6f;
        else
            attackDamageMultiplier = 0f;
    }

    public float GetAttackDamage()
    {
        return baseAttackDamage * attackDamageMultiplier;
    }

    public Limb.LimbSlot? TryStealLeastInconvenientLimb(out AttachedLimbData stolenData)
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
