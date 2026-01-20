using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

// 타일맵 Order In Layer가 높을 수록 앞으로 나온다.

public class TileMap2D : MonoBehaviour
{
    [Header("프리팹 참조 (PrefabData 폴더)")]
    [SerializeField] private Tile m_PrefabTable = null;
    private StageData m_StageData = null;

    private Tilemap m_BaseTilemap = null;
    public  Tilemap BaseTileMap => m_BaseTilemap;
    private Tilemap m_DecorationTilemap = null;
    private Tilemap m_HighlightTilemap = null;
    private GameObject m_Tiles = null; // 타일 오브젝트 그룹용 오브젝트

    [Header("하이라이트 생성 타일데이터")]
    [SerializeField] private TileBase m_NoneTile;
    [Header("하이라이트 컬러")]
    [SerializeField] private Color m_HighlightColor = Color.gray;

    private readonly List<Tile> m_ActiveTiles = new();
    static Dictionary<Vector2Int, Tile> m_DictionaryActiveTiles = new Dictionary<Vector2Int, Tile>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitStatic()
    {
        m_DictionaryActiveTiles = new Dictionary<Vector2Int, Tile>();
    }

    private ObjectPoolManager ObjectPoolManager = null;

    private void Awake()
    {
        ManagerHub.Instance.Register<TileMap2D>(this);
        SetupGridAndTilemap();
    }

    private void Start()
    {
        ObjectPoolManager ??= ManagerHub.Instance.GetManager<ObjectPoolManager>();
    }

    public void TileMapSettings(StageData _stageData)
    {
        m_StageData = _stageData;
        if(null !=  m_StageData)
        {
            ClearTiles(); 
            BuildObjectsFromTilemap();
            PaintDecorationTiles();
        }
        else
        {
            return;
        }
        
        foreach(var data in _stageData.MonsterSpawnPoints)
        {
            if(m_DictionaryActiveTiles.TryGetValue(data.Position, out Tile tile))
            {
                if(tile.TryGetComponent<MonsterSpawner>(out var monsterSpawner))
                {
                    if(null != data.MonsterSpawnData)
                    {
                        monsterSpawner.SetSpawnData(data.MonsterSpawnData);
                    }
                    else
                    {
                        DebugUtility.LogMessage(LogType.Warning, " 몬스터스폰 데이터가 없습니다.");
                    }        
                }
            }
        }

    }

    private void SetupGridAndTilemap()
    {
        var gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(transform);

        var grid = gridObj.AddComponent<Grid>();
        grid.cellSize = new Vector3(1, 1, 0);

        {   // 기본 타일맵
            var baseTilemapObj = new GameObject("BaseTilemap");
            baseTilemapObj.transform.SetParent(gridObj.transform);

            m_BaseTilemap = baseTilemapObj.AddComponent<Tilemap>();
            var tilemapRenderer = baseTilemapObj.AddComponent<TilemapRenderer>();
            tilemapRenderer.sortingOrder = 0;
        }

        {   // 장식용 타일맵
            var decorationTilemapObj = new GameObject("DecorationTilemap");
            decorationTilemapObj.transform.SetParent(gridObj.transform);

            m_DecorationTilemap = decorationTilemapObj.AddComponent<Tilemap>();
            var DecorationTilemapRenderer = decorationTilemapObj.AddComponent<TilemapRenderer>();
            DecorationTilemapRenderer.sortingOrder = 0;
        }

        {   // 하이라이트용 타일맵
            var highlightTilemap = new GameObject("HighlightTilemap");
            highlightTilemap.transform.SetParent(gridObj.transform);

            m_HighlightTilemap = highlightTilemap.AddComponent<Tilemap>();
            var DecorationTilemapRenderer = highlightTilemap.AddComponent<TilemapRenderer>();
            DecorationTilemapRenderer.sortingOrder = 2;
        }
    }

    public void BuildObjectsFromTilemap()
    {
        if (null == m_BaseTilemap || null == m_PrefabTable || null == m_StageData)
        {
            DebugUtility.LogMessage(LogType.Log, "BuildObjectsFromTilemap 실행중 데이터에 Null 있음");
            return;
        }

        if(m_ActiveTiles.Count > 0)
        {
            m_ActiveTiles.Clear();
        }

        Vector3Int origin = m_BaseTilemap.origin;
        Vector3 cellSize = m_BaseTilemap.cellSize;

        if(null == m_Tiles)
        {
            m_Tiles = new GameObject();
            m_Tiles.name = "Tiles";
            m_Tiles.transform.SetParent(transform);
        }

        ObjectPoolManager ??= ManagerHub.Instance.GetManager<ObjectPoolManager>();
        ObjectPoolManager.Register<Tile>(PoolKey.Tile, m_PrefabTable, m_Tiles.transform, 30);

        for (int y = 0; y < m_StageData.m_BaseTilemapHeight; y++)
        {
            for (int x = 0; x < m_StageData.m_BaseTilemapWidth; x++)
            {
                var tile = ObjectPoolManager.Spawn<Tile>(PoolKey.Tile);
                m_ActiveTiles.Add(tile); 

                Vector3Int cellPos = origin + new Vector3Int(x, y, 0);
                Vector3 worldPos = m_BaseTilemap.CellToWorld(cellPos) + cellSize * 0.5f;
                TileType type = m_StageData.GetCellType(x, y);
                // 타일 세팅 
                tile.transform.SetParent(m_Tiles.transform);
                tile.transform.position = worldPos;
                tile.transform.rotation = Quaternion.identity;
                tile.gameObject.name = $"Tile_{x}_{y}";
                Vector2Int vector2Int = new Vector2Int(x, y);
                tile.Init(this, vector2Int, type);
  
                // 페인팅 이미지 교체
                ChangeTile(cellPos, type);

                // 탐색용 딕션너리에 저장 
                m_DictionaryActiveTiles[vector2Int] = tile;
            }
        }

        // 추후 삭제 코드 (메인 카메라에 있는 카메라 컨트롤러 스크립트로 부터 카메라 위치 변경처리)
        var cameraController = Camera.main.GetComponent<CameraController>();
        if(null != cameraController)
        {
            cameraController.SetupCamera(m_BaseTilemap, m_StageData.m_BaseTilemapWidth, m_StageData.m_BaseTilemapHeight);
        }
        else
        {
            DebugUtility.LogMessage(LogType.Log, "cameraController Null입니다.");
        }
    }

    // 타입에 따른 타일맵 교체
    private void ChangeTile(Vector3Int _vector3Int, TileType _tileType)
    {
        switch(_tileType)
        {
            case TileType.None:
                if(null != m_BaseTilemap && null != m_StageData.m_NoneTile)
                {
                    m_BaseTilemap.SetTile(_vector3Int, m_StageData.m_NoneTile);
                }
                break;
            case TileType.MeleeCharacterSpawnPoint:
                if (null != m_BaseTilemap && null != m_StageData.m_NoneTile)
                {
                    m_BaseTilemap.SetTile(_vector3Int, m_StageData.m_MeleeTile);
                }
                break;
            case TileType.RangedCharacterSpawnPoint:
                if (null != m_BaseTilemap && null != m_StageData.m_NoneTile)
                {
                    m_BaseTilemap.SetTile(_vector3Int, m_StageData.m_RangedTile);
                }
                break;
            case TileType.MonsterSpawnPoint:
                if (null != m_BaseTilemap && null != m_StageData.m_NoneTile)
                {
                    m_BaseTilemap.SetTile(_vector3Int, m_StageData.m_MeleeTile);
                }
                break;
            case TileType.MonsterArrivalPoint:
                if (null != m_BaseTilemap && null != m_StageData.m_NoneTile)
                {
                    m_BaseTilemap.SetTile(_vector3Int, m_StageData.m_MeleeTile);
                }
                break;
        }
    }

    // 데코 타일맵 페인팅
    private void PaintDecorationTiles()
    {
        if (null == m_DecorationTilemap || null == m_StageData)
        {
            return;
        }

        Vector3Int origin = m_StageData.m_DecoTilePos;
        for (int y = 0; y < m_StageData.m_DecorationTilemapHeight; y++)
        {
            for (int x = 0; x < m_StageData.m_DecorationTilemapWidth; x++)
            {
                int idx = m_StageData.GetDecoIndex(x, y);
                if (idx < 0)
                {
                    continue;
                }

                int tileIndex = m_StageData.m_DecoCells[idx];

                // 0 = None 규칙
                if (tileIndex <= 0 || tileIndex >= m_StageData.m_DecorationTileList.Count)
                {
                    m_DecorationTilemap.SetTile(origin + new Vector3Int(x, y, 0), null);
                    continue;
                }

                TileBase tile = m_StageData.m_DecorationTileList[tileIndex];
                m_DecorationTilemap.SetTile(origin + new Vector3Int(x, y, 0), tile);
            }
        }
    }

    /// <summary>
    /// 리스트를 out 한 이유 : 몬스터가 넉백을 당해서 2칸 뒤로 갈때 스택 또는 큐로 했을때 인덱싱이 불가능하다. 
    /// 그래서 리스트로 해서 이동 경로를 주게 되었다.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public bool TryFindPathToNearestArrival(Vector2Int start, out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();

        int w = m_StageData.m_BaseTilemapWidth;
        int h = m_StageData.m_BaseTilemapHeight;

        bool[,] visited = new bool[w, h];
        Vector2Int[,] prev = new Vector2Int[w, h];
        Queue<Vector2Int> q = new Queue<Vector2Int>();

        q.Enqueue(start);
        visited[start.x, start.y] = true;

        Vector2Int found = new(-1, -1);

        Vector2Int[] dirs ={ Vector2Int.up, Vector2Int.right,
                             Vector2Int.down, Vector2Int.left};

        while (q.Count > 0)
        {
            var cur = q.Dequeue();

            if (m_StageData.GetBaseCellRaw(cur.x, cur.y) == TileType.MonsterArrivalPoint)
            {
                found = cur;
                break;
            }

            foreach (var d in dirs)
            {
                var next = cur + d;

                if (next.x < 0 || next.y < 0 || next.x >= w || next.y >= h)
                    continue;

                if (visited[next.x, next.y])
                    continue;

                var nextType = m_StageData.GetBaseCellRaw(next.x, next.y);

                if (!m_StageData.IsWalkable(next.x, next.y) && nextType != TileType.MonsterArrivalPoint)
                {
                    continue;
                }

                visited[next.x, next.y] = true;
                prev[next.x, next.y] = cur;
                q.Enqueue(next);
            }
        }

        if (found.x < 0)
        {
            found.x = 0;
        }

        // 경로 복원
        for (var p = found; p != start; p = prev[p.x, p.y])
        {
            path.Add(p);
        }

        path.Add(start);
        path.Reverse();
        return true;
    }

    // 타일맵 제거용
    private void ClearTiles()
    {
        if(null == ObjectPoolManager)
        {
            ObjectPoolManager = ManagerHub.Instance.GetManager<ObjectPoolManager>();
        }

        // 오브젝트 풀링에 반납
        foreach (var tile in m_ActiveTiles)
        {
            ObjectPoolManager.Release(PoolKey.Tile, tile);
        }

        m_ActiveTiles.Clear();
        m_DictionaryActiveTiles.Clear();

        m_BaseTilemap.ClearAllTiles();
        m_DecorationTilemap.ClearAllTiles();
    }

    static public Tile GetBaseTile(Vector2Int _vec)
    {
        if (true == m_DictionaryActiveTiles.TryGetValue(_vec, out var tile))
        {
            return tile;
        }

        return null;    
    }

    public Vector2Int GetCenterCellPosition()
    {
        if (null == m_BaseTilemap)
            return Vector2Int.zero;

        var bounds = m_BaseTilemap.cellBounds;

        int centerX = bounds.xMin + bounds.size.x / 2;
        int centerY = bounds.yMin + bounds.size.y / 2;

        return new Vector2Int(centerX, centerY);
    }

    private void HighlightTile(Vector2Int _pos)
    {
        if (m_BaseTilemap == null || m_StageData == null) return;

        var cell = new Vector3Int(_pos.x, _pos.y, 0);
        m_HighlightTilemap.SetTile(cell, m_NoneTile);
        m_HighlightTilemap.SetColor(cell, m_HighlightColor);     
    }

    public void SetColorHighlightTile(Vector2Int _pos, Color _color)
    {
        if (m_BaseTilemap == null || m_StageData == null) return;

        var cell = new Vector3Int(_pos.x, _pos.y, 0);
        m_HighlightTilemap.SetTile(cell, m_NoneTile);
        m_HighlightTilemap.SetColor(cell, _color);
    }

    // Highlight단일 타일 제거용 변경
    public void ClearTileHighlight(Vector2Int _pos)
    {
        if (null == m_BaseTilemap) return;
        var cell = new Vector3Int(_pos.x, _pos.y, 0);
        m_HighlightTilemap.SetTile(cell, null);
    }

    /// <summary>
    /// https://www.youtube.com/watch?v=8LtK6Drjq8s 참고
    /// 설치 위치 잡기용
    /// </summary>
    /// <param name="_targetType"></param>
    public void HighlightTilesByType(TileType _targetType)
    {
        foreach(var tile in m_ActiveTiles)
        {
            if(tile.TileType == _targetType)
            {
                HighlightTile(tile.CellPos);
            }
        }
    }

    public void ClearAllTileHighlights(TileType _targetType)
    {
        foreach (var tile in m_ActiveTiles)
        {
            if (tile.TileType == _targetType)
            {
                ClearTileHighlight(tile.CellPos);
            }
        }
    }
}
