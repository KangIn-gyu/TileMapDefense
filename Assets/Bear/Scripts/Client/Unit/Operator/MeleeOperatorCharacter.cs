using UnityEngine;

public class MeleeOperatorCharacter : BaseOperatorCharacter, IPoolable
{
    private void Update()
    {
        if(false == m_IsSetupComplete) return;

        m_AttackTimer += Time.deltaTime;
        Attack();
    }

    public override void Attack()
    {
        if (m_AttackTimer < m_OperatorData.m_AttackCooldown)
        {
            m_CharacterState = CharacterState.AttackCooldown;
            return;
        }

        if (null != m_OperatorData)
        {
            // 여기에 공격 애니메이션 같은게 있으면 좋을 거 같다.
            m_CharacterState = CharacterState.Attacking;
            if(true == TryFindAttackTarget(out var Tile))
            {
                Tile.DealDamageToMonster(m_OperatorData.m_AttackPower);
                m_AttackTimer = 0f;
            }
        }
    }

    public void OnDespawn()
    {

    }

    public void OnSpawn()
    {

    }
}
