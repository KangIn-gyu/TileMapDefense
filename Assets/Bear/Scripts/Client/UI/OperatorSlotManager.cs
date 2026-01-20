using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class OperatorSlotManager : MonoBehaviour, IInputReceiver
{
    [Header("OperatorSlot 슬롯 프리팹 추가")]
    [SerializeField] private OperatorSlot m_OperatorSlot = null;
    
    private Dictionary<OperatorData, OperatorSlot> m_DataToSlot = new();      // 오퍼레이션 데이터로 슬롯 찾기용
    private Dictionary<OperatorSlot, OperatorData> m_SlotToData = new();      // 오퍼레이션 데이터로 슬롯 찾기용
    private OperatorSlot[] m_Slots = new OperatorSlot[m_SlotCount];           // 슬롯 12개 만들기용
    [SerializeField]
    private List<OperatorSlot> m_ActiveSlots = new();                         // 활성화 슬롯 인덱싱용

    [Header("SlotIndex 인스펙터창에서 건들지 마시오.")]
    [SerializeField] private int m_SlotIndex = 0;
    private const int m_SlotCount = 12;
    private const float INPUT_DEADZONE = 0.5f;

    private InputHandlerManager InputHandlerManager = null;
    private FieldManager FieldManager = null;   
    private GameManager GameManager = null; 

    private void Awake()
    {
        ManagerHub.Instance.Register<OperatorSlotManager>(this);
        
        for(int i = 0; i < m_SlotCount; i++)
        {
            m_Slots[i] = Instantiate(m_OperatorSlot, transform);
            m_Slots[i].gameObject.SetActive(false);
        }

       // ManagerHub.Instance.GetManager<PlayerStateController>().RegisterInputReceiver(PlayerSituation.OperatorSlot, this);
    }

    private void Start()
    {
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        FieldManager ??= ManagerHub.Instance.GetManager<FieldManager>();
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
    }

    private void Update()
    {
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        if(null != GameManager)
        {
            foreach(var slot in m_ActiveSlots)
            {
                slot.SlotActivation(GameManager.GetCoin());
            }
        }
    }

    public void Init(IReadOnlyList<OperatorData> _datas)
    {
        m_ActiveSlots.Clear();
        m_DataToSlot.Clear();
        m_SlotToData.Clear();

        for (int i = 0; i < m_Slots.Length; i++)
        {
            m_Slots[i].ResetSlot();
        }

        if (null != _datas)
        {
            var sortedDatas = _datas
                 .OrderBy(d => d.m_Cost)
                 .ToList(); 

            int count = sortedDatas.Count;
            for (int i = 0; i < count; i++)
            {
                var slot = m_Slots[i];
                slot.gameObject.SetActive(true);
                slot.OperatorDataRegister(sortedDatas[i]);
                slot.SlotActivation(0);
                m_DataToSlot[sortedDatas[i]] = slot;
                m_SlotToData[slot] = sortedDatas[i];
                m_ActiveSlots.Add(slot);
            }
        }

        m_SlotIndex = 0;
        Invoke("InputReceiver", Time.fixedDeltaTime);
    }

    private void InputReceiver()
    {
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        InputHandlerManager.ReplaceInputReceiver(this);
    }

    public void RegisterToInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.RegisterCallback("Player", "Move", OnMove, ActionPhaseType.Performed);
        _inputHandler.RegisterCallback("Player", "FieldSlotToggleFocus", OnToggleFocus, ActionPhaseType.Performed);
        _inputHandler.RegisterCallback("Player", "Select", OnSelect, ActionPhaseType.Performed);

        if (m_ActiveSlots.Count > 0)
        {
            m_ActiveSlots[m_SlotIndex].CurrentSlot(true);
        }
    }

    public void UnregisterFromInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.UnregisterCallback("Player", "Move", OnMove, ActionPhaseType.Performed);
        _inputHandler.UnregisterCallback("Player", "FieldSlotToggleFocus", OnToggleFocus, ActionPhaseType.Performed);
        _inputHandler.UnregisterCallback("Player", "Select", OnSelect, ActionPhaseType.Performed);

        if(m_ActiveSlots.Count > 0)
        {
            m_ActiveSlots[m_SlotIndex].CurrentSlot(false);
        }
    }

    public void MoveSlot(int _direction) // direction: -1 or +1
    {
        if (m_ActiveSlots.Count == 0)
            return;

        m_ActiveSlots[m_SlotIndex].CurrentSlot(false);
        m_SlotIndex = (m_SlotIndex + _direction + m_ActiveSlots.Count) % m_ActiveSlots.Count;
        m_ActiveSlots[m_SlotIndex].CurrentSlot(true);
    }

#region 스폰 관련 함수
    private void OnSelect(InputAction.CallbackContext _ctx)
    {
        if(true == _ctx.performed)
        {
            if (m_ActiveSlots.Count > 0)
                SpawnOperatorOnField(m_ActiveSlots[m_SlotIndex]);
        }
    }

    public void SpawnOperatorOnField(OperatorSlot _slot)
    {
        if(false == _slot.CanCreate || 0 == m_ActiveSlots.Count)
        {
            return;
        }

        if (m_SlotToData.TryGetValue(_slot, out var data))
        {
            GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
            if (data.m_Cost <= GameManager.GetCoin())
            {
                if (null == _slot) return;

                _slot.CurrentSlot(false);
                _slot.gameObject.SetActive(false);

                int removedIndex = m_ActiveSlots.IndexOf(_slot);
                m_ActiveSlots.RemoveAt(removedIndex);

                if (m_ActiveSlots.Count > 0)
                {
                    m_SlotIndex = Mathf.Clamp(removedIndex, 0, m_ActiveSlots.Count - 1);
                    m_ActiveSlots[m_SlotIndex].CurrentSlot(true);
                }
                else
                {
                    m_SlotIndex = 0;
                }

                InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
                FieldManager ??= ManagerHub.Instance.GetManager<FieldManager>();

                FieldManager.EnableOperatorDeployment(data);
                InputHandlerManager.ReplaceInputReceiver(FieldManager);
            }
        }
    }

    // 쿨타임 적용 없이 반납
    public void RestoreOperatorSlot(OperatorData _data)
    {
        if (m_DataToSlot.TryGetValue(_data, out var slot))
        {
            slot.gameObject.SetActive(true);
            m_ActiveSlots.Add(slot);
        }
    }

    // 쿨타임 적용되게 반납
    public void ApplyRedeploymentCooldown(OperatorData _data)
    {
        if (m_DataToSlot.TryGetValue(_data, out var slot))
        {
            slot.gameObject.SetActive(true);
            slot.StartRedeploymentCooldown();
            m_ActiveSlots.Add(slot);
        }
    }
 #endregion

    private void OnMove(InputAction.CallbackContext _ctx)
    {
        float x = _ctx.ReadValue<Vector2>().x;
        if (x > INPUT_DEADZONE)
        {
            MoveSlot(1);
        }
        else if (x < -INPUT_DEADZONE)
        {
            MoveSlot(-1);
        }
    }

    private void OnToggleFocus(InputAction.CallbackContext _ctx)
    {
        if (_ctx.performed)
        {
            InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
            FieldManager ??= ManagerHub.Instance.GetManager<FieldManager>();

            InputHandlerManager.ReplaceInputReceiver(FieldManager);
        }
    }
}
