using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Resource Object Tile")]
public class ResourceObjectTile : Tile
{
    [Header("Visuals")]
    public Sprite decalSprite;
    public Color tileTint = Color.white;

    [Header("Prefab")]
    public GameObject pilePrefab; // <- you can ignore if you assign via Inspector as "Instanced Game Object"
    public Vector3 prefabOffset = new Vector3(0f,0.1f,0f);

    // overall size multiplier you can tweak per tile asset
    [Range(0.1f,2f)] public float prefabScaleMultiplier =0.7f;

    [Header("Randomization")]
    [Range(0f,45f)] public float rotationVariance =15f;
    [Range(0f,0.5f)] public float scaleVariance =0.06f;
    [Range(0f,0.5f)] public float positionJitter =0.08f;

    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        if (go != null)
        {
            // stable per-cell seed
            unchecked
            {
                uint h = (uint)(position.x *73856093) ^ (uint)(position.y *19349663);
                var rng = new System.Random((int)h);

                // Rotation
                float rot = Mathf.Lerp(-rotationVariance, rotationVariance, (float)rng.NextDouble());

                // Scale (uniform) — preserve prefab's initial scale
                float rand =1f + Mathf.Lerp(-scaleVariance, scaleVariance, (float)rng.NextDouble());
                float finalMul = prefabScaleMultiplier * rand;

                // Position jitter (in tile local space)
                float jx = Mathf.Lerp(-positionJitter, positionJitter, (float)rng.NextDouble());
                float jy = Mathf.Lerp(-positionJitter, positionJitter, (float)rng.NextDouble());

                var t = go.transform;
                var initial = t.localScale;
                t.localRotation = Quaternion.Euler(0f,0f, rot);
                t.localScale = new Vector3(initial.x * finalMul, initial.y * finalMul, initial.z);

                // Add jitter and configurable offset in local space
                t.localPosition += prefabOffset + new Vector3(jx, jy,0f);
            }
        }
        return base.StartUp(position, tilemap, go);
    }
}
