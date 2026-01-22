using System.Collections.Generic;
using UnityEngine;
public enum OperatorAttackType
{
    Operator_Melee,      // 근거리
    Operator_Ranged,     // 원거리
}
[CreateAssetMenu(fileName = "OperatorData", menuName = "Scriptable Objects/OperatorData")]
public class OperatorData : ScriptableObject
{
    public string             m_ID;                        // 데이터 아이디 
                                                           
    [Header("능력치")]                                      
    public float              m_HP = 0f;                   // 체력 
    public OperatorAttackType m_AttackType;                // 공격 타입
    public float              m_AttackPower = 0f;          // 공격력
    public float              m_Defense = 0f;              // 방어력
    public float              m_AttackCooldown = 0f;       // 공격 쿨타임

    [Header("비용")]
    public int                m_Cost = 0;                  // 비용 
    public float              RedeploymentMaxCooldown = 0; // 재배치 쿨타임

    [Header("이미지")]
    public Sprite             m_Sprite;                    // 캐릭터가 될지 아이콘이 될지 모름?
    public Sprite             m_ClassIcon;                 // 클래스 아이콘

    public List<Vector2Int>   m_AttackOffsets;

#if UNITY_EDITOR
    private void OnEnable()
    {
        OperatorDatabaseProvider.Register(this);
    }

    private void OnDisable()
    {
        OperatorDatabaseProvider.Unregister(this);
    }
#endif
}
