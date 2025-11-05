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
    public int seed =0;

    private GameObject[] _validVariants;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.colliderType = Tile.ColliderType.None;
        tileData.flags = TileFlags.LockColor; 
        tileData.sprite = previewSprite;

        var arr = _validVariants ?? variants;
        if (arr == null || arr.Length ==0)
            return;

        unchecked
        {
            int h = position.x *73856093 ^ position.y *19349663 ^ seed *83492791;
            int idx = (int)((uint)h % (uint)arr.Length);
            tileData.gameObject = arr[idx];
        }
    }

#if UNITY_EDITOR
    protected void OnValidate()
    {
        if (variants == null || variants.Length ==0)
        {
            _validVariants = null;
            return;
        }
        int count =0;
        for (int i =0; i < variants.Length; i++)
            if (variants[i] != null) count++;
        if (count == variants.Length)
        {
            _validVariants = null; 
            return;
        }
        var filtered = new GameObject[count];
        int j =0;
        for (int i =0; i < variants.Length; i++)
            if (variants[i] != null) filtered[j++] = variants[i];
        _validVariants = filtered;
    }
#endif
}