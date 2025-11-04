using System;
using UnityEngine;

[Serializable]
public struct ResourceCost
{
    public ResourceType type;
    public int amount;
}

public static class CostUtils
{
    public static bool CanAfford(ResourceBank bank, ResourceCost[] costs)
    {
        if (!bank || costs == null) return true;
        foreach (var c in costs)
            if (bank.Get(c.type) < c.amount) return false;
        return true;
    }

    public static void Spend(ResourceBank bank, ResourceCost[] costs)
    {
        if (!bank || costs == null) return;
        foreach (var c in costs)
            bank.Add(c.type, -c.amount);
    }
}