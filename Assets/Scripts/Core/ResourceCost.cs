using System;
using System.Collections.Generic;
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
        if (!bank || costs == null || costs.Length == 0)
            return true;

        var map = AggregateCosts(costs, out bool valid);
        if (!valid)
            return false;

        foreach (var kv in map)
        {
            if (bank.Get(kv.Key) < kv.Value)
                return false;
        }
        return true;
    }

    public static void Spend(ResourceBank bank, ResourceCost[] costs)
    {
        if (!bank || costs == null || costs.Length == 0)
            return;

        var map = AggregateCosts(costs, out bool valid);
        if (!valid)
            return;

        foreach (var kv in map)
            bank.Add(kv.Key, -kv.Value);
    }

    public static bool TrySpend(ResourceBank bank, ResourceCost[] costs)
    {
        if (!bank || costs == null || costs.Length == 0)
            return true;

        var map = AggregateCosts(costs, out bool valid);
        if (!valid)
            return false;

        foreach (var kv in map)
        {
            if (bank.Get(kv.Key) < kv.Value)
                return false;
        }
        foreach (var kv in map)
            bank.Add(kv.Key, -kv.Value);
        return true;
    }

    private static Dictionary<ResourceType, int> AggregateCosts(ResourceCost[] costs, out bool valid)
    {
        valid = true;
        var map = new Dictionary<ResourceType, int>();
        for (int i = 0; i < costs.Length; i++)
        {
            var c = costs[i];
            if (c.amount < 0)
            {
                valid = false; 
                continue;
            }
            if (c.amount == 0)
                continue;

            int sum;
            if (map.TryGetValue(c.type, out sum))
                map[c.type] = sum + c.amount;
            else
                map[c.type] = c.amount;
        }
        return map;
    }
}