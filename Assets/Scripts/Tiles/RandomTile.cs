using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(fileName = "New Random Tile", menuName = "Tiles/Random Tile")]
[ExecuteAlways]
public class RandomTile : TileBase
{
    [Header("Sprites")]
    [Tooltip("Standard sprites til jævn fordeling hvis 'variants' er tomt (fallback).")]
    public Sprite[] m_Sprites;

    [Header("Weighted Variants")]
    [Tooltip("Vægtede varianter (har forrang frem for 'm_Sprites').")]
    public WeightedSprite[] variants;

    [Header("Randomization")]
    [Tooltip("Seed for deterministisk randomisering. Samme seed => samme mønster.")]
    public int seed = 0;

    [Header("Tile Settings")]
    [Tooltip("Farve der multipliceres med sprite.")]
    public Color m_Color = Color.white;

    [NonSerialized] private Sprite[] _spritesNonNull;
    [NonSerialized] private Sprite[] _variantSprites;
    [NonSerialized] private int[] _variantCumWeights; 
    [NonSerialized] private int _variantTotalWeight;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.color = m_Color;
        tileData.flags = TileFlags.LockTransform;
        tileData.colliderType = Tile.ColliderType.None;

        if (_variantTotalWeight > 0 && _variantSprites != null && _variantCumWeights != null)
        {
            int h = Hash3(position.x, position.y, seed);
            int roll = (int)((uint)h % (uint)_variantTotalWeight);

            int idx = Array.BinarySearch(_variantCumWeights, roll);
            if (idx < 0) idx = ~idx; 
            tileData.sprite = _variantSprites[idx];
            return;
        }

        var sprites = _spritesNonNull ?? m_Sprites;
        if (sprites == null || sprites.Length == 0)
            return;

        int index = (int)((uint)Hash3(position.x, position.y, seed) % (uint)sprites.Length);
        var sprite = sprites[index];
        if (sprite == null)
        {
            for (int i = 1; i < sprites.Length; i++)
            {
                int j = (index + i) % sprites.Length;
                if (sprites[j] != null)
                {
                    sprite = sprites[j];
                    break;
                }
            }
            if (sprite == null) return;
        }
        tileData.sprite = sprite;
    }

    private static int Hash3(int x, int y, int s)
    {
        unchecked
        {
            uint h = 2166136261u;          
            h = (h ^ (uint)x) * 16777619u;
            h = (h ^ (uint)y) * 16777619u;
            h = (h ^ (uint)s) * 16777619u;
            return (int)h;
        }
    }


    protected void OnValidate()
    {
        RebuildCache();
    }

    [ContextMenu("Refresh Cache")]
    public void RefreshCache()
    {
        RebuildCache();
    }

    private void RebuildCache()
    {
        if (m_Sprites != null && m_Sprites.Length > 0)
        {
            var list = new List<Sprite>(m_Sprites.Length);
            for (int i = 0; i < m_Sprites.Length; i++)
            {
                var s = m_Sprites[i];
                if (s != null) list.Add(s);
            }
            _spritesNonNull = list.Count > 0 ? list.ToArray() : null;
        }
        else
        {
            _spritesNonNull = null;
        }

        if (variants != null && variants.Length > 0)
        {
            var spr = new List<Sprite>(variants.Length);
            var cum = new List<int>(variants.Length);
            int total = 0;
            for (int i = 0; i < variants.Length; i++)
            {
                var v = variants[i];
                if (v.sprite == null) continue;
                int w = v.weight < 1 ? 1 : v.weight;
                total += w;
                spr.Add(v.sprite);
                cum.Add(total);
            }

            if (total > 0 && spr.Count > 0)
            {
                _variantSprites = spr.ToArray();
                _variantCumWeights = cum.ToArray();
                _variantTotalWeight = total;
            }
            else
            {
                _variantSprites = null;
                _variantCumWeights = null;
                _variantTotalWeight = 0;
            }
        }
        else
        {
            _variantSprites = null;
            _variantCumWeights = null;
            _variantTotalWeight = 0;
        }
    }

    [System.Serializable]
    public struct WeightedSprite
    {
        [Tooltip("Sprite for varianten.")]
        public Sprite sprite;
        [Range(1, 100)] [Tooltip("Vægt (1-100). Højere = oftere valgt.")]
        public int weight;
    }
}