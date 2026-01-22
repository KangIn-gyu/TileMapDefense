using System;
using UnityEngine;

public class MonsterSpawnManager : MonoBehaviour
{
    [Header("PrefabData 폴더애서 프리팹 참조")]
    [SerializeField] private MeleeMonster m_MeleeMonsterPrefab = null;

    [Header("Database폴더에 MonsterDatabase 참조")]
    [SerializeField] private MonsterDatabase m_MonsterDatabase = null;

    public  MonsterDatabase MonsterDatabase => m_MonsterDatabase;
    private ObjectPoolManager ObjectPoolManager = null;

    public event Action OnSpawnRequested; // 몬스터 소환 이벤트

    private GameObject m_MeleeMonsterGroup = null;

    private GameManager GameManager = null;

    private void Awake()
    {
        ManagerHub.Instance.Register<MonsterSpawnManager>(this);
        ObjectPoolManager = ManagerHub.Instance.GetManager<ObjectPoolManager>();

        m_MeleeMonsterGroup = new GameObject();
        m_MeleeMonsterGroup.name = "MeleeMonsterGroup";
        m_MeleeMonsterGroup.transform.SetParent(transform);

        InitializeMonsterPools();
    }

    private void Start()
    {
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        GameManager.m_UnitRemovalEvent += RecoverAllOperators;
    }

    public MonsterData FindById(string _id)
    {
        if(null == m_MonsterDatabase)
        {
            return null;
        }

        return m_MonsterDatabase.FindById(_id);
    }

    private void InitializeMonsterPools()
    {
        if (m_MeleeMonsterPrefab == null)
            return;

        // 근접 몬스터 처리
        ObjectPoolManager.Register<MeleeMonster>(
            MonsterAttackType.Monster_Melee,
            m_MeleeMonsterPrefab,
            m_MeleeMonsterGroup.transform,
            30
        );
    }

    public void SummoningBegins()
    {
        if(null != OnSpawnRequested)
        {
            OnSpawnRequested.Invoke();
        }
    }

    private void RecoverAllOperators()
    {
        ObjectPoolManager ??= ManagerHub.Instance.GetManager<ObjectPoolManager>();
        ObjectPoolManager.ReleaseAll(MonsterAttackType.Monster_Melee);
        ObjectPoolManager.ReleaseAll(MonsterAttackType.Monster_Ranged);
    }
}
