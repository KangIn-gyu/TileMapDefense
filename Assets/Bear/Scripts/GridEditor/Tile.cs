using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TileType
{
    None,
    MeleeCharacterSpawnPoint,  // 근접 캐릭터
    RangedCharacterSpawnPoint, // 원거리 캐릭터
    MonsterSpawnPoint,         // 몬스터 소환 포인트
    MonsterArrivalPoint        // 몬스터 도착 포인트
}

public class Tile : MonoBehaviour, IPoolable
{
    private TileMap2D m_TileMap = null;
    [SerializeField] private TileType m_TileType;
    public TileType TileType => m_TileType;
    [SerializeField] private Vector2Int m_CellPos = Vector2Int.one; // 나중에 지워야 됨.
    public Vector2Int CellPos => m_CellPos;

    [SerializeField] private BaseOperatorCharacter m_BaseOperatorCharacter = null;
    [SerializeField] private LinkedList<BaseMonster> m_BaseMonsters = new();
    public void Init(TileMap2D _tileMap, Vector2Int _cellPos, TileType _type)
    {
        m_TileMap = _tileMap;
        m_CellPos = _cellPos;
        m_TileType = _type;

        if(TileType.MonsterSpawnPoint == m_TileType)
        {
            var monsterSpawn = this.AddComponent<MonsterSpawner>();
            monsterSpawn.Init();
        }

        m_BaseOperatorCharacter = null;
    }

    public bool TryRequestPath(out List<Vector2Int> _path)
    {
        _path = null;

        if (TileType.MonsterSpawnPoint != m_TileType)
            return false;

        if (null == m_TileMap)
            return false;

        return m_TileMap.TryFindPathToNearestArrival(m_CellPos, out _path);
    }

    public Tilemap GetTileMap ()
    {
        return m_TileMap.BaseTileMap;
    }

    // 해당 타일의 캐릭터에게 데미지 전달함수 
    public void DealDamageToCharacter(float _damage)
    {
        if(null != m_BaseOperatorCharacter)
        {
            m_BaseOperatorCharacter.TakeDamage(_damage);
        }
    }

    // 해당 타일의 몬스터에게 데미지 전달 함수
    public void DealDamageToMonster(float _damage)
    {
        if (m_BaseMonsters == null || m_BaseMonsters.Count == 0)
            return;

        // TODO : 나중에 변경이 필요 | 한 타일에 여러명 공격하는 캐릭터 생기면 구조 변경 필요
        m_BaseMonsters.First.Value.TakeDamage(_damage);
    }

    public bool HasOperatorCharacter()
    {
        return (null != m_BaseOperatorCharacter) ? true : false;
    }

    public bool HasMonster()
    {
        return m_BaseMonsters != null && m_BaseMonsters.Count > 0;
    }

    public void SetOperatorCharacter(BaseOperatorCharacter _character)
    {
        m_BaseOperatorCharacter = _character;
    }

    public void AddMonster(BaseMonster _baseMonster)
    {
        if (_baseMonster.CurrentTileNode != null)
            return;

        var node = m_BaseMonsters.AddLast(_baseMonster);
        _baseMonster.CurrentTileNode = node;
        _baseMonster.CurrentTile = this;
    }

    public void RemoveMonster(BaseMonster _baseMonster)
    {
        if (_baseMonster.CurrentTileNode == null)
            return;

         m_BaseMonsters.Remove(_baseMonster.CurrentTileNode);
        _baseMonster.CurrentTileNode = null;
    }

    /// <summary>
    /// 풀링 시스템 도입으로 오브젝트가 재활용되면서,
    /// 이전 생명주기에서 사용된 MonsterSpawn 컴포넌트가
    /// 다음 사용 시점까지 남아 있는 문제가 발생할 수 있다.
    /// 
    /// 현재 구조에서는 MonsterSpawn이 필요하지 않은 경우가 많아,
    /// 완전한 초기 상태를 보장하기 위해 생성(Spawn) 시점에
    /// 해당 컴포넌트가 존재하면 제거하는 방식을 사용한다.
    /// 
    /// 컴포넌트 제거는 다소 비효율적일 수 있으나,
    /// 풀링 재사용으로 인한 예기치 않은 동작을 방지하기 위한
    /// 명시적인 안정화 처리이다.
    /// </summary>
    public void OnSpawn()
    {
        MonsterSpawner monsterSpawn = null;
        if (true == TryGetComponent<MonsterSpawner>(out monsterSpawn))
        {
            Destroy(monsterSpawn);
        }
    }

    public void OnDespawn() { }
}
