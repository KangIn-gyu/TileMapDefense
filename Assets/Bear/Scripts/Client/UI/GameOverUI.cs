using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameOverUI : MonoBehaviour, IPanelUI, IInputReceiver
{
    [Header("자식 오브젝트 참조")]
    [SerializeField] private Image m_ReplayArrowUI = null;
    [SerializeField] private Image m_HomeArrowUI = null;

    private const float INPUT_DEADZONE = 0.5f;
    private bool m_IsReplay = true;

    private InputHandlerManager InputHandlerManager = null;  
    private UIManager UIManager = null;
    private GameManager GameManager = null; 

    private void Awake()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.PanelUIRegister(UIType.GameOver, this);
    }

    private void Start()
    {
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
    }

    public void Open()
    {
        SetChildrenActive(true);
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        InputHandlerManager.ReplaceInputReceiver(this);
        m_IsReplay = true;
        UpdateArrowUI();
    }

    public void Close()
    {
        SetChildrenActive(false);
    }

    private void SetChildrenActive(bool isActive)
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(isActive);
        }
    }

    public void RegisterToInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.RegisterCallback("UI", "Move", OnMove, ActionPhaseType.Started);
        _inputHandler.RegisterCallback("UI", "Select", OnSelect, ActionPhaseType.Started);
    }

    public void UnregisterFromInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.UnregisterCallback("UI", "Move", OnMove, ActionPhaseType.Started);
        _inputHandler.UnregisterCallback("UI", "Select", OnSelect, ActionPhaseType.Started);
    }

    private void OnMove(InputAction.CallbackContext _ctx)
    {
        float y = _ctx.ReadValue<Vector2>().y;
        if (y > INPUT_DEADZONE || y < -INPUT_DEADZONE)
        {
            ToggleSelection();
        }
    }

    private void ToggleSelection()
    {
        m_IsReplay = !m_IsReplay;
        UpdateArrowUI();
    }

    private void UpdateArrowUI()
    {
        if (null != m_ReplayArrowUI)
            m_ReplayArrowUI.enabled = m_IsReplay;

        if (null != m_HomeArrowUI)
            m_HomeArrowUI.enabled = !m_IsReplay;
    }

    private void OnSelect(InputAction.CallbackContext _ctx)
    {
        Select();
    }

    private void Select()
    {
        if(true == m_IsReplay)
        {
            GameManager?.m_UnitRemovalEvent?.Invoke();
            GameManager?.RePlay();
        }
        else    
        {
            UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
            UIManager.SetPanelActive(UIType.Stage, _isActive : true);
            UIManager.SetPanelActive(UIType.GamePlayUI, _isActive: false);
            GameManager?.m_UnitRemovalEvent?.Invoke();
        }

        Close();
    }
}
