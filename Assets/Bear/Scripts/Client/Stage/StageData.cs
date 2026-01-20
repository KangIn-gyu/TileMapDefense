using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileMapType
{
    Base,
    Deco
}

[Serializable]
public struct MonsterSpawnPoint
{
    public Vector2Int Position;
    public MonsterSpawnData MonsterSpawnData;
}

[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    public int m_PlayerLife = 0;                        // 플레이어 라이프
    public int m_MaxDeployableCharacters = 0;           // 스테이지에 배치 가능한 캐릭터 최대수
    public List<OperatorData> m_StageOperators = new(); // 해당 스테이지에서 사용 가능한 오퍼레이터

    public int m_BaseTilemapWidth = 8;                  // 타일맵 사이즈
    public int m_BaseTilemapHeight = 8;
    public TileType[] m_BaseCells = null;

    public AutoTile m_NoneTile = null;
    public AutoTile m_RangedTile = null;
    public AutoTile m_MeleeTile = null;

    [Tooltip("장식용 타일맵 시작점")]
    public Vector3Int m_DecoTilePos;
    public int m_DecorationTilemapWidth = 9;
    public int m_DecorationTilemapHeight = 9;

    [Tooltip("코드로 참조하지마시오. 인스펙터에서만 처리")]
    public GameObject m_TilePalette;
    [Header("건들지 마시오. (TilePalette 설정하면 자동으로 됨.)")]
    public List<TileBase> m_DecorationTileList= new();
    public int[] m_DecoCells = null;

    [SerializeField, HideInInspector]
    public GameObject m_LastTilePalette;

    [Header("Monster Spawn Cache")]
    [SerializeField] private List<MonsterSpawnPoint> m_MonsterSpawnPoints = new();
    public IReadOnlyList<MonsterSpawnPoint> MonsterSpawnPoints => m_MonsterSpawnPoints;

    public TileType GetCellType(int _x, int _y)
    {
        if (null == m_BaseCells)
        {
            return TileType.None;
        }
        int idx = _y * m_BaseTilemapWidth + _x;

        if (idx < 0 || idx >= m_BaseCells.Length)
        {
            return TileType.None;
        }
        return m_BaseCells[idx];
    }
    public TileType GetBaseCellRaw(int x, int y)
    {
        if (null == m_BaseCells)
        {
            return TileType.None;
        }

        if (x < 0 || y < 0 || x >= m_BaseTilemapWidth || y >= m_BaseTilemapHeight)
        {
            return TileType.None;
        }

        int idx = y * m_BaseTilemapWidth + x;

        if (idx < 0 || idx >= m_BaseCells.Length)
        {
            return TileType.None;
        }

        return m_BaseCells[idx];
    }

    public void SetBaseCellRaw(int x, int y, TileType type)
    {
        if (null == m_BaseCells)
        {
            return;
        }

        if (x < 0 || y < 0 || x >= m_BaseTilemapWidth || y >= m_BaseTilemapHeight)
        {
            return;
        }

        int idx = y * m_BaseTilemapWidth + x;

        if (idx < 0 || idx >= m_BaseCells.Length)
        {
            return;
        }
        m_BaseCells[idx] = type;
    }

    public int GetDecoIndex(int x, int y)
    {
        if (x < 0 || y < 0 || x >= m_DecorationTilemapWidth || y >= m_DecorationTilemapHeight)
        {
            return -1;
        }

        int idx = y * m_DecorationTilemapWidth + x;

        if (m_DecoCells == null || idx >= m_DecoCells.Length)
        {
            return -1;
        }

        return idx;
    }

    private void ResizeTileMap<T>(ref T[] cells, int oldW, int oldH, int newW, int newH)
    {
        var newCells = new T[newW * newH];

        if (cells == null || cells.Length == 0 || oldW <= 0 || oldH <= 0)
        {
            cells = newCells;
            return;
        }

        int safeOldW = oldW;
        int safeOldH = oldH;

        // oldW*oldH 와 실제 배열 길이가 안 맞으면 보정
        if (safeOldW * safeOldH != cells.Length)
        {
            // oldW가 이상하면(0이거나 음수) 복사 포기
            if (safeOldW <= 0)
            {
                cells = newCells;
                return;
            }

            // cells.Length가 0이면 위에서 return 됐지만, 방어적으로 한 번 더
            int cl = cells.Length;
            safeOldW = Mathf.Min(safeOldW, cl);

            // safeOldW가 0이 되면 나눗셈 불가 → 복사 포기
            if (safeOldW <= 0)
            {
                cells = newCells;
                return;
            }

            safeOldH = cl / safeOldW;

            // safeOldH도 0이면 복사 의미 없음
            if (safeOldH <= 0)
            {
                cells = newCells;
                return;
            }
        }

        int copyW = Mathf.Min(safeOldW, newW);
        int copyH = Mathf.Min(safeOldH, newH);

        // copyW/copyH가 0이면 루프 의미 없음
        if (copyW <= 0 || copyH <= 0)
        {
            cells = newCells;
            return;
        }

        for (int y = 0; y < copyH; y++)
        {
            int newRow = y * newW;
            int oldRow = y * safeOldW;

            for (int x = 0; x < copyW; x++)
            {
                int oldIdx = oldRow + x;
                int newIdx = newRow + x;

                // oldIdx 범위 체크
                if ((uint)oldIdx >= (uint)cells.Length) // 빠르고 안전한 체크
                    continue;

                newCells[newIdx] = cells[oldIdx];
            }
        }

        cells = newCells;
    }

    private void OnValidate()
    {
        int baseSize = Mathf.Max(1, m_BaseTilemapWidth) * Mathf.Max(1, m_BaseTilemapHeight);
        if (m_BaseCells == null || m_BaseCells.Length != baseSize)
        {
            int oldW = m_BaseCells == null ? 1 : Mathf.Max(1, m_BaseTilemapWidth);
            int oldH = m_BaseCells == null ? 1 : Mathf.Max(1, m_BaseTilemapHeight);

           ResizeTileMap(
           ref m_BaseCells,
           oldW,
           oldH,
           Mathf.Max(1, m_BaseTilemapWidth),
           Mathf.Max(1, m_BaseTilemapHeight)
           );

        }

        int decoSize = Mathf.Max(1, m_DecorationTilemapWidth) * Mathf.Max(1, m_DecorationTilemapHeight);
        if (m_DecoCells == null || m_DecoCells.Length != decoSize)
        {
            int oldW = m_DecoCells == null ? 1 : Mathf.Max(1, m_DecorationTilemapWidth);
            int oldH = m_DecoCells == null ? 1 : Mathf.Max(1, m_DecorationTilemapHeight);
     
           ResizeTileMap(
           ref m_DecoCells,
           oldW,
           oldH,
           Mathf.Max(1, m_DecorationTilemapWidth),
           Mathf.Max(1, m_DecorationTilemapHeight)
           );
         }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    // 에디터용
    public void RebuildMonsterSpawnPoints()
    {
        m_MonsterSpawnPoints.Clear();

        for (int y = 0; y < m_BaseTilemapHeight; y++)
        {
            for (int x = 0; x < m_BaseTilemapWidth; x++)
            {
                if (GetBaseCellRaw(x, y) == TileType.MonsterSpawnPoint)
                {
                    var MonsterSpawnPoint = new MonsterSpawnPoint();
                    MonsterSpawnPoint.Position = new Vector2Int(x, y);
                    m_MonsterSpawnPoints.Add(MonsterSpawnPoint);
                }
            }
        }
    }

    public bool IsWalkable(int _x, int _y)
    {
        TileType type = GetBaseCellRaw(_x, _y); 
        return (type == TileType.MeleeCharacterSpawnPoint) ? true : false; 
    }

    public int GetTotalSpawnCount()
    {
        int total = 0;  
        foreach(var point in m_MonsterSpawnPoints)
        {
            total += point.MonsterSpawnData.SpawnList.Count;
        }

        return total;
    }
}
