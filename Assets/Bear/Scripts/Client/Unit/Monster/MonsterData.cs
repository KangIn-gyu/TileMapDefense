using System.Collections.Generic;
using UnityEngine;

public enum MonsterAttackType
{
    Monster_Melee,      // 근거리
    Monster_Ranged,     // 원거리
}

[CreateAssetMenu(fileName = "MonsterData", menuName = "Scriptable Objects/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string           m_ID;                       // 데이터 아이디   

    [Header("능력치")]
    public float             m_HP = 0f;                 // 체력 
    public MonsterAttackType m_AttackType;              // 공격 타입
    public float             m_AttackPower = 0f;        // 공격력
    public float             m_Defense = 0f;            // 방어력
    public float             m_AttackCooldown = 0f;     // 공격 쿨타임

    [Header("이동")]
    public float             m_MoveTime;                // 한 타일 이동하는 시간
    public float             m_MovementCooldown;        // 이동후 대기 시간
    public int               m_TotalMoves;              // 이동 횟수

    [Header("이미지")]
    public Sprite            m_Sprite;                  // 텍스쳐

    [HideInInspector]
    public Vector2Int[] AttackDirection =
    {
       Vector2Int.up,
       Vector2Int.down,
       Vector2Int.left,
       Vector2Int.right
    };

    /// <summary>
    /// m_AttackOffsets 에디터 커스텀을 통해 자신의 오프셋과 가장 가까운 것부터 정렬되게 처리했음.
    /// </summary>
    public List<Vector2Int> m_AttackOffsets;

#if UNITY_EDITOR
    private void OnEnable()
    {
        MonsterDatabaseProvider.Register(this);
    }

    private void OnDisable()
    {
        MonsterDatabaseProvider.Unregister(this);
    }
#endif
}
