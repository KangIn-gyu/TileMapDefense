using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private List<Vector2Int> m_MovementPath;
    [SerializeField] private MonsterSpawnData m_MonsterSpawnData = null;
    private Tile m_Tile = null;

    private MonsterSpawnManager MonsterSpawnManager = null;
    private int m_Index = 0;
    private float m_Timer = 0f;
    private bool m_IsRunning = false;
    private Coroutine m_SpawnRoutine = null;

    private void Awake()
    {
        if (null == m_Tile)
        {
            m_Tile = GetComponent<Tile>();
        }
    }

    private void OnDisable()
    {
        if (MonsterSpawnManager != null)
            MonsterSpawnManager.OnSpawnRequested -= BeginSpawn;
    }

    /// <summary>
    /// 타일클래스에서 호출해서 초기화 진행
    /// </summary>
    public void Init()
    {
        if(null == m_Tile)
        {
            m_Tile = GetComponent<Tile>();
        }

        m_Tile.TryRequestPath(out m_MovementPath);

        // 이벤트 구독 
        if (null == MonsterSpawnManager)
        {
            MonsterSpawnManager = ManagerHub.Instance.GetManager<MonsterSpawnManager>();
        }
        MonsterSpawnManager.OnSpawnRequested += BeginSpawn;
    }

    public void BeginSpawn()
    {
        if (m_SpawnRoutine != null)
            StopCoroutine(m_SpawnRoutine);

        m_SpawnRoutine = StartCoroutine(SpawnRoutine());
    }

    public void SetSpawnData(MonsterSpawnData _data)
    {
        m_MonsterSpawnData = _data; 
    }

    private IEnumerator SpawnRoutine()
    {
        if (m_MonsterSpawnData == null ||
            m_MonsterSpawnData.SpawnList.Count == 0 ||
            m_MovementPath == null || m_MovementPath.Count == 0)
            yield break;

        //  스테이지 시작 딜레이
        if (m_MonsterSpawnData.DelayTime > 0f)
            yield return new WaitForSeconds(m_MonsterSpawnData.DelayTime);

        //  몬스터 순차 소환
        foreach (var entry in m_MonsterSpawnData.SpawnList)
        {
            if (entry.Delay > 0f)
                yield return new WaitForSeconds(entry.Delay);

            SpawnOne(entry.MonsterID);
        }

        m_SpawnRoutine = null; // 웨이브 종료
    }

    /// <summary>
    /// 단일 소환
    /// </summary>
    /// <param name="_monsterId"></param>
    private void SpawnOne(string _monsterId)
    {
        if (MonsterSpawnManager == null)
            MonsterSpawnManager = ManagerHub.Instance.GetManager<MonsterSpawnManager>();

        var monsterData = MonsterSpawnManager.FindById(_monsterId);
        if (monsterData == null) 
            return;

        BaseMonster monster = SpawnByAttackType(monsterData.m_AttackType);
        if (monster == null) 
            return;

        m_Tile.AddMonster(monster);
        monster.Init(m_MovementPath, m_Tile.GetTileMap(), monsterData);
    }


    private BaseMonster SpawnByAttackType(MonsterAttackType attackType)
    {
        var pool = ManagerHub.Instance.GetManager<ObjectPoolManager>();

        switch (attackType)
        {
            case MonsterAttackType.Monster_Melee:
                return pool.Spawn<MeleeMonster>(attackType);

            case MonsterAttackType.Monster_Ranged:
                return null;

            default:
                return null;
        }
    }
}
