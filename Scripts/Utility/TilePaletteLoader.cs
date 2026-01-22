using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilePaletteLoader
{
    private static GameObject m_TilePalettePrefab = null;

    public static List<TileBase> LoadTilesFromPalette(GameObject _tilePalettePrefab)
    {
        m_TilePalettePrefab = _tilePalettePrefab;
        
        if (m_TilePalettePrefab == null)
        {
            DebugUtility.LogMessage(LogType.Log, "타일 팔레트 프리팹이 설정되지 않았습니다.");
            return null;
        }

        List<TileBase> tiles = new List<TileBase>();

        // 프리팹의 자식 오브젝트를 순회하며 Tilemap 컴포넌트를 검색
        foreach (Transform child in m_TilePalettePrefab.transform)
        {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                // 타일맵에서 모든 타일 가져오기
                BoundsInt bounds = tilemap.cellBounds;
                TileBase[] allTiles = tilemap.GetTilesBlock(bounds);

                foreach (TileBase tile in allTiles)
                {
                    if (tile != null && !tiles.Contains(tile))
                    {
                        tiles.Add(tile);
                    }
                }
            }
        }
        return tiles;
    }

    public static Sprite GetTileSprite(GameObject _tilePalettePrefab, TileBase _tileBase)
    {
        m_TilePalettePrefab = _tilePalettePrefab;

        if (m_TilePalettePrefab == null)
        {
            DebugUtility.LogMessage(LogType.Log, "타일 팔레트 프리팹이 설정되지 않았습니다.");
            return null;
        }

        foreach (Transform child in m_TilePalettePrefab.transform)
        {
            Tilemap tilemap = child.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                BoundsInt bounds = tilemap.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    TileBase tile = tilemap.GetTile(pos);
                    if (tile == _tileBase)
                    {
                        return tilemap.GetSprite(pos);
                    }
                }
            }
        }

        return null;
    }
}
