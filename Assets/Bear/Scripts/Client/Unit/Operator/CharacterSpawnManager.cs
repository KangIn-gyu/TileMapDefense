using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSpawnManager : MonoBehaviour, IInputReceiver
{
    [Header("PrefabData 폴더애서 프리팹 참조")]
    [SerializeField] private MeleeOperatorCharacter m_MeleePrefab = null;  // 근거리 프리팹
    [SerializeField] private RangedOperatorCharacter m_RangedPrefab = null; // 원거리 프리팹

    private GameObject m_MeleeCharacterGroup = null;

    private ObjectPoolManager ObjectPoolManager = null;
    private OperatorSlotManager OperatorSlotManager = null;
    private InputHandlerManager InputHandlerManager = null;
    private FieldManager FieldManager = null;
    private GameManager GameManager = null; 
    private UIManager UIManager = null;
    private AttackDirUI AttackDirUI = null;

    private Vector2Int m_dir = Vector2Int.zero;
    private BaseOperatorCharacter m_BaseOperatorCharacter = null;
    private Tile m_Tile = null;

    private void Awake()
    {
        ManagerHub.Instance.Register<CharacterSpawnManager>(this);

        m_MeleeCharacterGroup = new GameObject();
        m_MeleeCharacterGroup.name = "MeleeCharacterGroup";
        m_MeleeCharacterGroup.transform.SetParent(transform);
    }

    private void Start()
    {
        ObjectPoolManager ??= ManagerHub.Instance.GetManager<ObjectPoolManager>();
        OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        FieldManager ??= ManagerHub.Instance.GetManager<FieldManager>();
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        if (null != UIManager)
        {
            AttackDirUI = UIManager.AttackDirUI;
        }

        ObjectPoolManager.Register<MeleeOperatorCharacter>(OperatorAttackType.Operator_Melee, m_MeleePrefab, m_MeleeCharacterGroup.transform, 8);
        ObjectPoolManager.Register<RangedOperatorCharacter>(OperatorAttackType.Operator_Ranged, m_RangedPrefab, transform, 8);

        GameManager.m_UnitRemovalEvent += RecoverAllOperators;
    }

    public bool Spawn(Vector2Int _vec, OperatorData _data)
    {
        if(null == _data)
        {
            return false;
        }

        ObjectPoolManager ??= ManagerHub.Instance.GetManager<ObjectPoolManager>();

        m_Tile = TileMap2D.GetBaseTile(_vec);
        if(null != m_Tile)
        if (false == m_Tile.HasOperatorCharacter())
        {
            if(OperatorAttackType.Operator_Melee == _data.m_AttackType && 
                    TileType.MeleeCharacterSpawnPoint == m_Tile.TileType)
            {
                    m_BaseOperatorCharacter = ObjectPoolManager.Spawn<MeleeOperatorCharacter>(_data.m_AttackType);
            }
            else if(OperatorAttackType.Operator_Ranged == _data.m_AttackType && 
                    TileType.RangedCharacterSpawnPoint == m_Tile.TileType)
            {
                    m_BaseOperatorCharacter = ObjectPoolManager.Spawn<RangedOperatorCharacter>(_data.m_AttackType);
            }
            else
            {
                OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();
                OperatorSlotManager.RestoreOperatorSlot(_data);
                return false;
            }

            m_BaseOperatorCharacter.Init(_data, _vec);

            // 여기서 타일을 통해 공격 범위 보이는거 추가
            InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
            InputHandlerManager.ReplaceInputReceiver(this);

            UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
            if (null != UIManager)
            {
                AttackDirUI = UIManager.AttackDirUI;
                if (null != AttackDirUI)
                {
                    AttackDirUI.Open();
                    AttackDirUI.SetTarget(m_BaseOperatorCharacter.transform);
                }
            }

            return true;
        }
        else
        {
            // 해당 타일에 캐릭터가 있는 경우 다시 반납하는거 추가해야됨.
            OperatorSlotManager ??= ManagerHub.Instance.GetManager<OperatorSlotManager>();
            OperatorSlotManager.RestoreOperatorSlot(_data);
         }

        return false;
    }

    public void RegisterToInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.RegisterCallback("Player", "AttackDirectionSelect", OnMove, ActionPhaseType.Performed);
        _inputHandler.RegisterCallback("Player", "AttackDirectionSelect", OnMove, ActionPhaseType.Canceled);
    }
    public void UnregisterFromInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.UnregisterCallback("Player", "AttackDirectionSelect", OnMove, ActionPhaseType.Performed);
        _inputHandler.UnregisterCallback("Player", "AttackDirectionSelect", OnMove, ActionPhaseType.Canceled);

        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        GameManager.RestoreGameSpeed();
    }

    private void OnMove(InputAction.CallbackContext _ctx)
    {
        if (true == _ctx.performed)
        {
            var input = _ctx.ReadValue<Vector2>();
            if (input.sqrMagnitude < 0.01f)
                return;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                m_dir = input.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                m_dir = input.y > 0 ? Vector2Int.up : Vector2Int.down;

            UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
            if(null != UIManager)
            {
                AttackDirUI = UIManager.AttackDirUI;
                if (null != AttackDirUI)
                {
                    AttackDirUI.SetDirection(m_dir);
                    m_BaseOperatorCharacter.ClearAttackRangeTiles();
                    m_BaseOperatorCharacter.AttackDirectionSelection(m_dir);
                    m_BaseOperatorCharacter.AttackRangeTileProduction();
                }
            }
        }
        else if (true == _ctx.canceled)
        {
            OperatorAttackDirectionSelection(m_dir);

            UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
            if (null != UIManager)
            {
                AttackDirUI = UIManager.AttackDirUI;
                if (null != AttackDirUI)
                {
                    AttackDirUI.SetDirection(Vector2.zero);
                    AttackDirUI.Close();
                }
            }

            m_dir = Vector2Int.zero;
            m_Tile.SetOperatorCharacter(m_BaseOperatorCharacter);
            m_BaseOperatorCharacter = null;
            m_Tile = null;
        }
    }

    private void OperatorAttackDirectionSelection(Vector2Int _dir)
    {
        if(null != m_BaseOperatorCharacter)
        {
            m_BaseOperatorCharacter.AttackDirectionSelection(m_dir);
            m_BaseOperatorCharacter.ClearAttackRangeTiles();
            m_BaseOperatorCharacter.CreationCompleted();
        }

        FieldManager ??= ManagerHub.Instance.GetManager<FieldManager>();
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();

        InputHandlerManager.ReplaceInputReceiver(FieldManager);
    }

    private void RecoverAllOperators()
    {
        ObjectPoolManager ??= ManagerHub.Instance.GetManager<ObjectPoolManager>();
        ObjectPoolManager.ReleaseAll(OperatorAttackType.Operator_Melee);
        ObjectPoolManager.ReleaseAll(OperatorAttackType.Operator_Ranged);
    }
}
