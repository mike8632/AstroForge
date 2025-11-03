using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/Random GameObject Tile")]
public class RandomGameObjectTile : TileBase
{
    [Tooltip("Prefabs to choose from per cell.")]
    public GameObject[] variants;

    [Tooltip("Optional sprite shown in the Tile Palette (not used at runtime).")]
    public Sprite previewSprite;

    [Tooltip("Stable seed for deterministic selection.")]
    public int seed = 0;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.colliderType = Tile.ColliderType.None;
        tileData.sprite = previewSprite; 

        if (variants == null || variants.Length == 0)
            return;


        unchecked
        {
            int hash = position.x * 73856093 ^ position.y * 19349663 ^ seed * 83492791;
            int idx = Mathf.Abs(hash) % variants.Length;
            tileData.gameObject = variants[idx];
        }
    }
}