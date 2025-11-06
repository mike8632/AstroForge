using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlacementRule
{
 Anywhere,
 OnResourceOnly,
 OnGroundOnly 
}

[CreateAssetMenu(menuName="AstroForge/Buildable")]
public class BuildableDefinition : ScriptableObject
{
 public string displayName;
 public GameObject prefab;

 [Tooltip("Footprint in cells (X width, Y height). E.g.,1x1 for belts,2x2 for miner.")]
 public Vector2Int footprint = new Vector2Int(1,1);

 [Tooltip("What cells are allowed.")]
 public PlacementRule placementRule = PlacementRule.Anywhere;

 [Tooltip("If placementRule=OnResourceOnly, restrict to these resource types.")]
 public ResourceType[] allowedResourceTypes;

 [Tooltip("Cost to place once.")]
 public ResourceCost[] cost;

#if UNITY_EDITOR
 private void OnValidate()
 {
 if (footprint.x <1) footprint.x =1;
 if (footprint.y <1) footprint.y =1;

 if (cost != null)
 {
 for (int i =0; i < cost.Length; i++)
 {
 if (cost[i].amount <0) cost[i].amount =0;
 }
 }

 if (allowedResourceTypes != null && allowedResourceTypes.Length >1)
 {
 var set = new HashSet<ResourceType>();
 var list = new List<ResourceType>(allowedResourceTypes.Length);
 for (int i =0; i < allowedResourceTypes.Length; i++)
 {
 var t = allowedResourceTypes[i];
 if (set.Add(t)) list.Add(t);
 }
 if (list.Count != allowedResourceTypes.Length)
 allowedResourceTypes = list.ToArray();
 }

 if (string.IsNullOrWhiteSpace(displayName) && prefab != null)
 displayName = prefab.name;
 }
#endif
}