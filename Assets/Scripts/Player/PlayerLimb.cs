using System.Collections.Generic;
using UnityEngine;

public class PlayerLimb : MonoBehaviour
{
    [Header("Current Limbs")]
    public List<Limb> equippedLimbs = new List<Limb> { };

    [Header("Stats")]
    public float baseWalkSpeed = 8f;
    public float baseSprintSpeed = 12f;
    public float baseAttackDamage = 100f; // 2 arms = one-shot, 1 arm = two-shot 

    public float moveSpeedMultiplier = 1f;
    public float attackDamageMultiplier = 1f;

    public int CurrentArmCount { get; private set; }
    public int CurrentLegCount { get; private set; }

    private Limb.LimbType? lastSnatchedType = null;

    void Start()
    {
        if (equippedLimbs == null || equippedLimbs.Count == 0)
        {
            equippedLimbs = new List<Limb>();
            
            for (int i = 0; i < 2; i++)
            {
                GameObject armObj = new GameObject();
                armObj.hideFlags = HideFlags.HideAndDontSave;
                Limb arm = armObj.AddComponent<Limb>();
                arm.limbType = Limb.LimbType.Arm;
                arm.isEquipped = true;
                equippedLimbs.Add(arm);
            }
            
            for (int i = 0; i < 2; i++)
            {
                GameObject legObj = new GameObject();
                legObj.hideFlags = HideFlags.HideAndDontSave;
                Limb leg = legObj.AddComponent<Limb>();
                leg.limbType = Limb.LimbType.Leg;
                leg.isEquipped = true;
                equippedLimbs.Add(leg);
            }
        }

        RecalculateStats();
    }

    public void RecalculateStats()
    {
        int legCount = 0;
        int armCount = 0;

        foreach (var limb in equippedLimbs)
        {
            if (limb == null) continue;
            if (limb.limbType == Limb.LimbType.Leg)
                legCount++;
            else if (limb.limbType == Limb.LimbType.Arm)
                armCount++;
        }

        if (legCount >= 2)
            moveSpeedMultiplier = 1f;
        else if (legCount == 1)
            moveSpeedMultiplier = 0.45f;
        else
            moveSpeedMultiplier = 0f;

        if (armCount >= 2)
            attackDamageMultiplier = 1f;
        else if (armCount == 1)
            attackDamageMultiplier = 0.6f;
        else
            attackDamageMultiplier = 0f;

        CurrentArmCount = armCount;
        CurrentLegCount = legCount;
        
    }

    public float GetAttackDamage()
    {
        return baseAttackDamage * attackDamageMultiplier;
    }

    public bool TryRemoveLimb(out Limb removed)
    {
        removed = null;

        if (equippedLimbs == null || equippedLimbs.Count == 0)
            return false;

        Limb.LimbType targetType = DetermineTargetLimbType();

        for (int i = 0; i < equippedLimbs.Count; i++)
        {
            if (equippedLimbs[i] != null && equippedLimbs[i].limbType == targetType)
            {
                removed = equippedLimbs[i];
                equippedLimbs.RemoveAt(i);
                break;
            }
        }

        if (removed == null && equippedLimbs.Count > 0)
        {
            removed = equippedLimbs[0];
            equippedLimbs.RemoveAt(0);
        }

        if (removed != null)
        {
            removed.isEquipped = false;
            lastSnatchedType = removed.limbType;
            RecalculateStats();
            return true;
        }

        return false;
    }

    private Limb.LimbType DetermineTargetLimbType()
    {
        int availableArms = CurrentArmCount;
        int availableLegs = CurrentLegCount;

        if (lastSnatchedType == null)
            return availableArms > 0 ? Limb.LimbType.Arm : Limb.LimbType.Leg;

        Limb.LimbType nextType = lastSnatchedType == Limb.LimbType.Arm 
            ? Limb.LimbType.Leg 
            : Limb.LimbType.Arm;

        if ((nextType == Limb.LimbType.Arm && availableArms > 0) ||
            (nextType == Limb.LimbType.Leg && availableLegs > 0))
            return nextType;

        return availableArms > 0 ? Limb.LimbType.Arm : Limb.LimbType.Leg;
    }
}
