using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class BaseOperatorCharacter : MonoBehaviour, IUnit
{
    protected bool m_IsSetupComplete = false;

    [SerializeField] protected OperatorData m_OperatorData = null;        // 유닛 정보
    [SerializeField] private   float m_HP;                                // 몬스터 HP
    [SerializeField] protected float m_AttackTimer = 0f;                  // 공격 타이머
    [SerializeField] protected AttackDirection m_AttackDirection;         // 현재 내 공격방향

    // 스프라이트 처리용 필드
    private SpriteRenderer m_SpriteRenderer = null;
    private Vector3 m_OriginScale;

    // 설치된 위치
    private Vector2Int m_CurrentLocation = Vector2Int.one;

    private ObjectPoolManager ObjectPoolManager = null;
    private OperatorSlotManager OperatorSlotManager = null;
    protected enum CharacterState
    {
        Idle,          //  가만히
        Attacking,     //  공격중
        AttackCooldown //  공격 쿨타임 충전중 
    }
    [SerializeField] protected CharacterState m_CharacterState;
    [SerializeField] protected Tilemap m_BaseTilemap;

    [Header("타격 연출")]
    [SerializeField] private float m_HitEffectDuration = 0.2f;
    private Tween m_HitTween = null;
    private Color m_OriginColor;

    // 공격 범위 타일 처리용 필드
    private TileMap2D TileMap2D = null;
    private readonly List<Vector2Int> m_CurrentHighlightedTiles = new();

    virtual public void Attack(){}

    public void Awake()
    {
        m_OriginScale = transform.localScale;
        TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
    }

    public void TakeDamage(float _damage)
    {
        m_HP -= _damage;
        HitEffect();
        if (m_HP <= 0)
        {
            Death();
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

    protected Vector3 GridToWorld(Vector2Int _cell)
    {
        var tile = TileMap2D.GetBaseTile(_cell);
        if (null != tile)
        {
            if (null == m_BaseTilemap)
            {
                m_BaseTilemap = tile.GetTileMap();
            }
        }

        Vector3Int cellPos = new Vector3Int(_cell.x, _cell.y, 0);
        Vector3 world = m_BaseTilemap.CellToWorld(cellPos);
        world.z = -1f;
        return world + m_BaseTilemap.cellSize * 0.5f;
    }

    public void Init(OperatorData _data, Vector2Int _vec)
    {
        m_OperatorData = _data;
        ResetState(); // 능력치

        transform.localScale = m_OriginScale;
        transform.position = GridToWorld(_vec);
        m_CurrentLocation = _vec;

        m_AttackTimer = _data.m_AttackCooldown;

        if (null != m_OperatorData)
        {
            m_SpriteRenderer ??= GetComponent<SpriteRenderer>();
            m_SpriteRenderer.sprite = m_OperatorData.m_Sprite;
            Bounds bounds = m_SpriteRenderer.sprite.bounds;
            float spriteHeight = bounds.size.y;               // 월드 단위
            float targetHeight = m_BaseTilemap.cellSize.y;    // 타일 1칸 높이
            float scale = targetHeight / spriteHeight;
            transform.localScale = Vector3.one * scale;
            m_OriginColor = m_SpriteRenderer.color;
        }
    }

    protected virtual void ResetState()
    {
        m_IsSetupComplete = false;

        if (null != m_OperatorData)
        {
            m_HP = m_OperatorData.m_HP;
        }

        m_CharacterState = CharacterState.Idle;
    }

    public void AttackDirectionSelection(Vector2Int _dir)
    {
        if (Vector2Int.up == _dir)
            m_AttackDirection = AttackDirection.UP;
        else if (Vector2Int.down == _dir)
            m_AttackDirection = AttackDirection.DOWN;
        else if (Vector2Int.left == _dir)
            m_AttackDirection = AttackDirection.LEFT;
        else if (Vector2Int.right == _dir)
            m_AttackDirection = AttackDirection.RIGHT;
    }

    // 공격범위 타일연출
    public void AttackRangeTileProduction()
    {
        foreach (var baseOffset in m_OperatorData.m_AttackOffsets)
        {
            Vector2Int rotatedOffset = RotateOffset(baseOffset, m_AttackDirection);
            Vector2Int pos = m_CurrentLocation + rotatedOffset;

            if (!m_BaseTilemap.HasTile((Vector3Int)pos))
                continue;

            Tile tile = TileMap2D.GetBaseTile(pos);
            if (tile == null)
                continue;

            TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
            TileMap2D.SetColorHighlightTile(pos, Color.red);
            m_CurrentHighlightedTiles.Add(pos);
        }
    }

    // 이전 공격범위 색칠 제거 
    public void ClearAttackRangeTiles()
    {
        TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
        foreach (var pos in m_CurrentHighlightedTiles)
        {
            TileMap2D.ClearTileHighlight(pos); // 원래 색
        }

        m_CurrentHighlightedTiles.Clear();
    }


    protected void Death()
    {
        // 여기다 죽는 애니메이션 처리 있으면 좋을거 같다.
        if(null == ObjectPoolManager)
        {
            ObjectPoolManager = ManagerHub.Instance.GetManager<ObjectPoolManager>();
        }

        ObjectPoolManager.Release(m_OperatorData.m_AttackType, this);
        TileMap2D.GetBaseTile(m_CurrentLocation).SetOperatorCharacter(null);

        OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();
        OperatorSlotManager.ApplyRedeploymentCooldown(m_OperatorData);
    }

    protected Vector2Int RotateOffset(Vector2Int _offset, AttackDirection _dir)
    {
        Vector2Int forward;
        Vector2Int right;

        switch (_dir)
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
        return forward * _offset.x + right * _offset.y;
    }


    protected bool TryFindAttackTarget(out Tile _targetTile)
    {
        foreach (var baseOffset in m_OperatorData.m_AttackOffsets)
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
                 if (true == tile.HasMonster())
                 {
                     _targetTile = tile;
                     return true;
                 }             
            }
        }

        _targetTile = null;
        return false;
    }

    // 용도 : 캐릭터 스포매니저가 공격 방향 완료처리 되면 호출됨.
    public void CreationCompleted()
    {
        m_IsSetupComplete = true;   
    }
}
