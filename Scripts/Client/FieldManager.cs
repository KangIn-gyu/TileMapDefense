using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// 타일을 이동을 관리
/// </summary>
public class FieldManager : MonoBehaviour, IInputReceiver
{
    [Header("건들지 마시오.(디버그)")]
    [SerializeField] private Vector2Int m_CurrentLocation = Vector2Int.zero;

    [Header("건들지 마시오.(디버그)")]
    [SerializeField] private bool m_CanDeployOperator = false;

    private StageData m_StageData = null;
    private OperatorData m_OperatorData = null;

    private GameManager GameManager = null;
    private TileMap2D TileMap2D = null;
    private OperatorSlotManager OperatorSlotManager = null;
    private InputHandlerManager InputHandlerManager = null;
    private CharacterSpawnManager CharacterSpawnManager = null;
    private UIManager UIManager = null;

    private void Awake()
    {
        ManagerHub.Instance.Register<FieldManager>(this);
    }

    private void Start()
    {
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
        CharacterSpawnManager ??= ManagerHub.Instance.GetManager<CharacterSpawnManager>();
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
    }

    public void EnableOperatorDeployment(OperatorData _operatorData)
    {
        if(null == _operatorData)
        {
            return;
        }

        m_CanDeployOperator = true;
        m_OperatorData = _operatorData;

        // 여기서 설치가 가능한 타일맵에 변화는 주는 기능을 넣어야 됨.
        TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
        if(OperatorAttackType.Operator_Melee == m_OperatorData.m_AttackType)
        {
            TileMap2D.HighlightTilesByType(TileType.MeleeCharacterSpawnPoint);
        }
        else if(OperatorAttackType.Operator_Ranged == m_OperatorData.m_AttackType)
        {
            TileMap2D.HighlightTilesByType(TileType.RangedCharacterSpawnPoint);
        }

        // 게임 스피드 조정
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        GameManager.SlowGameSpeed();
    }

    public void Init(StageData _stageData)
    {
        m_StageData = _stageData;
        m_CurrentLocation = TileMap2D.GetCenterCellPosition(); // 중심 좌표 생성
        m_CanDeployOperator = false;
    }

    public void RegisterToInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.RegisterCallback("Player", "Move", OnMove, ActionPhaseType.Performed);
        _inputHandler.RegisterCallback("Player", "FieldSlotToggleFocus", OnToggleFocus, ActionPhaseType.Performed);
        _inputHandler.RegisterCallback("Player", "Select", OnSelect, ActionPhaseType.Performed);

        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.FieldArrowUI.Open();
        UIManager.FieldArrowUI.OnValueChanged(m_CurrentLocation);
    }

    public void UnregisterFromInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.UnregisterCallback("Player", "Move", OnMove, ActionPhaseType.Performed);
        _inputHandler.UnregisterCallback("Player", "FieldSlotToggleFocus", OnToggleFocus, ActionPhaseType.Performed);
        _inputHandler.RegisterCallback("Player", "Select", OnSelect, ActionPhaseType.Performed);

        m_CanDeployOperator = false;
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.FieldArrowUI.Close();
    }

    private void OnMove(InputAction.CallbackContext _ctx)
    {
        var input = _ctx.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.01f)
            return;

        Vector2Int dir = Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            dir = input.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            dir = input.y > 0 ? Vector2Int.up : Vector2Int.down;

        Move(dir);
    }

    private void Move(Vector2Int _dir)
    {
        int width = m_StageData.m_BaseTilemapWidth;
        int height = m_StageData.m_BaseTilemapHeight;

        Vector2Int next = m_CurrentLocation + _dir;

        // 좌우 래핑
        if (next.x < 0) 
            next.x = width - 1;
        else if (next.x >= width) 
            next.x = 0;

        // 상하 래핑
        if (next.y < 0) 
            next.y = height - 1;
        else if (next.y >= height) 
            next.y = 0;

        m_CurrentLocation = next;
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.FieldArrowUI.OnValueChanged(m_CurrentLocation);
    }

    private void OnToggleFocus(InputAction.CallbackContext _ctx)
    {
        if (true == _ctx.performed)
        {
            InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
            OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();

            InputHandlerManager.ReplaceInputReceiver(OperatorSlotManager);
            m_CanDeployOperator = false;
            
            // 설치 가능한 상태에서 키로 전환되었을때 처리
            if(null != m_OperatorData)
            {
                OperatorSlotManager.RestoreOperatorSlot(m_OperatorData);
                TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
                switch (m_OperatorData.m_AttackType)
                {
                    case OperatorAttackType.Operator_Melee:
                        TileMap2D.ClearAllTileHighlights(TileType.MeleeCharacterSpawnPoint);
                        break;
                    case OperatorAttackType.Operator_Ranged:
                        TileMap2D.ClearAllTileHighlights(TileType.RangedCharacterSpawnPoint);
                        break;
                }
                
                m_OperatorData = null;
                GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
                GameManager.RestoreGameSpeed();
            }
        }
    }

    private void OnSelect(InputAction.CallbackContext _ctx)
    {
        if (true == _ctx.performed)
        {
            Select();
            InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
            OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();
        }
    }

    private void Select()
    {
        if(true == m_CanDeployOperator)
        {
            CharacterSpawnManager ??= ManagerHub.Instance.GetManager<CharacterSpawnManager>();
            if(null != m_OperatorData)
            {
                if(true == CharacterSpawnManager.Spawn(m_CurrentLocation, m_OperatorData))
                {
                    GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
                    GameManager.UseCoin(m_OperatorData.m_Cost);
                }
            }
            else
            {
                return;
            }

            TileMap2D ??= ManagerHub.Instance.GetManager<TileMap2D>();
            switch(m_OperatorData.m_AttackType)
            {
                case OperatorAttackType.Operator_Melee:
                    TileMap2D.ClearAllTileHighlights(TileType.MeleeCharacterSpawnPoint);
                    break;
                case OperatorAttackType.Operator_Ranged:
                    TileMap2D.ClearAllTileHighlights(TileType.RangedCharacterSpawnPoint);
                    break;
            }

            m_OperatorData = null;
        }
        else
        {
            // 해당 타일의 캐릭터 정보를 보는 기능 넣어야 됨.
        }
    }
}
