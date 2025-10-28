// Assets/Tools/Tilemap/RefreshAllTiles.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapTools
{
    [MenuItem("Tools/Tilemap/Refresh All Tiles")]
    public static void RefreshAll()
    {
        // Ny API i Unity 6: FindObjectsByType
        var tilemaps = Object.FindObjectsByType<Tilemap>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (var tm in tilemaps)
            tm.RefreshAllTiles();

        Debug.Log($"Refreshed {tilemaps.Length} Tilemap(s)");
    }
}
#endif
