using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class BaseMonster : MonoBehaviour, IUnit
{
    [SerializeField] protected MonsterData         m_MonsterData;                       // 유닛 정보

    [SerializeField] protected float               m_HP;                                // 몬스터 HP
    [SerializeField] protected float               m_AttackTimer = 0f;                  // 공격 타이머
    [SerializeField] protected Vector2Int          m_CurrentLocation = Vector2Int.one;  // 그리드상 현재 위치
    [SerializeField] protected AttackDirection     m_AttackDirection;                   // 현재 내 공격방향
    private SpriteRenderer m_SpriteRenderer = null;

    // 이동 경로 타일 
    [SerializeField] private Tile m_CurrentTile;
    public Tile CurrentTile
    {
        get => m_CurrentTile;
        set => m_CurrentTile = value;
    }
    public LinkedListNode<BaseMonster> CurrentTileNode { get; set; }

    protected enum MonsterState
    {
        Idle,          //  가만히
        Moving,        //  이동중
        MoveCooldown,  //  이동 쿨타임 충전중
        Attacking,     //  공격중
        AttackCooldown //  공격 쿨타임 충전중 
    }
    [SerializeField] protected MonsterState m_MonsterState = MonsterState.Idle;

    [Header("이동")]
    protected                  int              m_MovesUsedInBatch = 0; // 배치에서 사용된 이동 횟수   
    [SerializeField] protected List<Vector2Int> m_Path;                 // 이동 경로
    protected                  int              m_PathIndex = 0;        // 이동 인덱스
    public bool IsMove { get; set; } = false;

    // 타격 연출
    [Header("타격 연출")]
    [SerializeField] private float m_HitEffectDuration = 0.2f;
    private Tween m_HitTween;
    private Color m_OriginColor;

    [SerializeField] protected Tilemap m_BaseTilemap;

    private GameManager GameManager = null;
    private ObjectPoolManager ObjectPoolManager = null;


    public void Init(List<Vector2Int> _path, Tilemap _tilemap, MonsterData _data)
    {
        m_MonsterData = _data;

        if (null != m_MonsterData)
        {
            if(null ==  m_SpriteRenderer)
            {
                m_SpriteRenderer = GetComponent<SpriteRenderer>();
                m_SpriteRenderer.sprite = m_MonsterData.m_Sprite;
                Bounds bounds = m_SpriteRenderer.sprite.bounds;
                float spriteHeight = bounds.size.y;          // 월드 단위
                float targetHeight = _tilemap.cellSize.y;    // 타일 1칸 높이
                float scale = targetHeight / spriteHeight;
                transform.localScale = Vector3.one * scale;
            }
            else
            {
                m_SpriteRenderer.sprite = m_MonsterData.m_Sprite;
                m_OriginColor = m_SpriteRenderer.color;
            }
        }

        if(null !=  _tilemap)
        {
            m_BaseTilemap = _tilemap;
        }

        m_Path = _path;

        ResetState();
    }

    protected virtual void ResetState()
    {
        if (null != m_MonsterData)
        {
            m_HP = m_MonsterData.m_HP;
        }

        m_MonsterState = MonsterState.Idle;
        m_PathIndex = 0;
        m_MovesUsedInBatch = 0;
        m_AttackTimer = 0f;
        IsMove = false;
    }

    public virtual void Attack() { }  // 공격
    public void TakeDamage(float _damage)
    {
        m_HP -= _damage;
        HitEffect();
        if (m_HP <= 0)
        {
            Death();
            GameManager.MonsterKill();
        }
    }

    private void HitEffect()
    {
        if (m_SpriteRenderer == null)
            return;

        // 이전 히트 연출 중단
        m_HitTween?.Kill();

        m_SpriteRenderer.color = Color.white;

        m_HitTween = m_SpriteRenderer
            .DOColor(Color.red, m_HitEffectDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad);
    }

    public virtual void Move() { } // 이동 
    protected void Death()
    {
        StopAllCoroutines();

        // 여기다 죽는 애니메이션 처리 있으면 좋을거 같다.
        if (null == ObjectPoolManager)
        {
            ObjectPoolManager = ManagerHub.Instance.GetManager<ObjectPoolManager>();
        }

        if(null != CurrentTile)
        {
            CurrentTile.RemoveMonster(this);
            CurrentTile = null; 
        }

        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        GameManager.GameClear();
        ObjectPoolManager.Release(m_MonsterData.m_AttackType, this);
    }

    private bool IsFinalDestination()
    {
        return m_PathIndex >= m_Path.Count - 1;
    }

    protected void ArrivedAtDestination()
    {
        if(null == GameManager)
        {
            GameManager = ManagerHub.Instance.GetManager<GameManager>();
        }

        if (true == IsFinalDestination())
        {
            GameManager.LifeDecrease();
            Death();
        }
    }

    protected void GetAttackDirection()
    {
        if (m_PathIndex >= m_Path.Count - 1)
            return;

        Vector2Int dir = m_Path[m_PathIndex + 1] - m_Path[m_PathIndex];

        if (Vector2.up == dir) m_AttackDirection = AttackDirection.UP;
        else if (Vector2Int.down == dir) m_AttackDirection = AttackDirection.DOWN;
        else if (Vector2Int.left == dir) m_AttackDirection = AttackDirection.LEFT;
        else if (Vector2Int.right == dir) m_AttackDirection = AttackDirection.RIGHT;
    }

    protected Vector2Int RotateOffset(Vector2Int offset, AttackDirection dir)
    {
        Vector2Int forward;
        Vector2Int right;

        switch (dir)
        {
            case AttackDirection.UP:
                forward = Vector2Int.up;
                right = Vector2Int.right;
                break;

            case AttackDirection.DOWN:
                forward = Vector2Int.down;
                right = Vector2Int.left;
                break;

            case AttackDirection.LEFT:
                forward = Vector2Int.left;
                right = Vector2Int.down;
                break;

            case AttackDirection.RIGHT:
                forward = Vector2Int.right;
                right = Vector2Int.up;
                break;

            default:
                forward = Vector2Int.left;
                right = Vector2Int.down;
                break;
        }

        // offset.x : 전진(음수 = 앞으로)
        // offset.y : 측면(오른쪽)
        return forward * (-offset.x) + right * offset.y;
    }

    protected Vector3 GridToWorld(Vector2Int _cell)
    {
        Vector3Int cellPos = new Vector3Int(_cell.x, _cell.y, 0);
        Vector3 world = m_BaseTilemap.CellToWorld(cellPos);
        world.z = -1f;
        return world + m_BaseTilemap.cellSize * 0.5f;
    }

    protected bool TryFindAttackTarget(out Tile targetTile)
    {
        foreach(var baseOffset in m_MonsterData.m_AttackOffsets)
        {
            Vector2Int rotatedOffset = RotateOffset(baseOffset, m_AttackDirection);
            Vector2Int check = m_CurrentLocation + rotatedOffset;

            if (!m_BaseTilemap.HasTile((Vector3Int)check))
                continue;

            var tileBase = m_BaseTilemap.GetTile((Vector3Int)check);
            if (tileBase == null)
                continue;

            Tile tile = TileMap2D.GetBaseTile(check); 
            if (null != tile)
            {
                if(MonsterAttackType.Monster_Melee == m_MonsterData.m_AttackType)
                {
                    if (TileType.MeleeCharacterSpawnPoint == tile.TileType)
                    {
                        if(true == tile.HasOperatorCharacter())
                        {
                            targetTile = tile;
                            return true;
                        }
                    }
                }
                else
                {
                    if (TileType.MeleeCharacterSpawnPoint == tile.TileType || 
                        TileType.RangedCharacterSpawnPoint == tile.TileType)
                    {
                        if (true == tile.HasOperatorCharacter())
                        {
                            targetTile = tile;
                            return true;
                        }
                    }
                }
            }
        }

        targetTile = null;
        return false;
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        if (m_BaseTilemap == null)
            return;

        // 기준 위치 (현재 타일 중심)
        Vector3 origin = GridToWorld(m_CurrentLocation);

        // 공격 방향 벡터
        Vector3 dir = m_AttackDirection switch
        {
            AttackDirection.UP => Vector3.up,
            AttackDirection.DOWN => Vector3.down,
            AttackDirection.LEFT => Vector3.left,
            AttackDirection.RIGHT => Vector3.right,
            _ => Vector3.zero
        };

        float length = m_BaseTilemap.cellSize.y * 0.8f;
        Vector3 end = origin + dir * length;

        // 본체 화살표 색
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, end);

        // 화살촉
        Vector3 right = Quaternion.Euler(0, 0, 30) * -dir;
        Vector3 left = Quaternion.Euler(0, 0, -30) * -dir;

        float headSize = length * 0.3f;
        Gizmos.DrawLine(end, end + right * headSize);
        Gizmos.DrawLine(end, end + left * headSize);
    }
#endif
}
