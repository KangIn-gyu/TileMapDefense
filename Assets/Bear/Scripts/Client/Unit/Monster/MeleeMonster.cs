using UnityEngine;
using System.Collections;

public class MeleeMonster : BaseMonster, IPoolable
{
    private Tile m_Tile = null; // 공격용임


    private void Update()
    {
        m_AttackTimer += Time.deltaTime;
        Move();
    }

    public override void Attack() 
    {
        if (m_AttackTimer < m_MonsterData.m_AttackCooldown)
        {
            m_MonsterState = MonsterState.AttackCooldown;
            return;
        }

        if(null != m_MonsterData)
        {
            // 여기에 공격 애니메이션 같은게 있으면 좋을 거 같다.
            m_MonsterState = MonsterState.Attacking;
            m_AttackTimer = 0f;
            m_Tile.DealDamageToCharacter(m_MonsterData.m_AttackPower);
        }
    }

    public override void Move() 
    {
        if (true == IsMove) 
            return;

        if (MonsterState.Idle != m_MonsterState) 
            return;

        IsMove = true;
        StartCoroutine(MoveBatch());
    }

    protected IEnumerator MoveOneTile()
    {
        Vector2Int fromCell = m_Path[m_PathIndex];
        Vector2Int toCell = m_Path[m_PathIndex + 1];

        Vector3 fromPos = GridToWorld(fromCell);
        Vector3 toPos = GridToWorld(toCell);
        GetAttackDirection(); // 이동하기 전 공격 방향 
        m_CurrentLocation = fromCell;

        Tile fromTile = TileMap2D.GetBaseTile(fromCell);
        Tile toTile = TileMap2D.GetBaseTile(toCell);

        CurrentTile = fromTile;

        if (true == TryFindAttackTarget(out m_Tile))
        {
            m_MonsterState = MonsterState.Attacking;

            while (m_Tile != null && m_Tile.HasOperatorCharacter())
            {
                Attack();
                yield return null; 
            }

            m_MonsterState = MonsterState.Idle;
            yield break;
        }

        m_MonsterState = MonsterState.Moving;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / m_MonsterData.m_MoveTime;
            transform.position = Vector3.Lerp(fromPos, toPos, t); 
            yield return null;
        }

        transform.position = toPos;

        if (toTile != null && fromTile != null)
        {
            fromTile.RemoveMonster(this);
            toTile.AddMonster(this);
        }

        m_PathIndex++;
        m_CurrentLocation = toCell;

        ArrivedAtDestination();
    }

    protected IEnumerator MoveBatch()
    {
        m_MonsterState = MonsterState.Moving;
        m_MovesUsedInBatch = 0;

        while (m_MovesUsedInBatch < m_MonsterData.m_TotalMoves)
        {
            if (m_PathIndex >= m_Path.Count - 1)
                break;

            yield return StartCoroutine(MoveOneTile());
            m_MovesUsedInBatch++;
        }

        // 여기서만 쿨다운
        m_MonsterState = MonsterState.MoveCooldown;
        yield return new WaitForSeconds(m_MonsterData.m_MovementCooldown);

        m_MonsterState = MonsterState.Idle;
        IsMove = false;
    }

    public void OnSpawn()
    {

    }

    public void OnDespawn(){}
}
