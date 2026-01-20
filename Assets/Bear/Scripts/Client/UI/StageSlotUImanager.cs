using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StageSlotUImanager : MonoBehaviour, IInputReceiver, IPanelUI
{
    [Header("UI (자식오브젝트 참조)")]
    [SerializeField] private ScrollRect m_Scroll;
    [SerializeField] private RectTransform m_Content;

    [Header("Slots (StageSlot 참조)")]
    [SerializeField] private List<StageSlotUI> m_Slot = new();

    [Header("Tween (연출)")]
    [SerializeField] private float m_MoveDuration = 0.4f;
    [SerializeField] private Ease m_Ease = Ease.OutCubic;

    [Header("디버그 건들지 마시오.")]
    [SerializeField] private int m_CurrentIndex = 0;

    [Header("닌텐도 매니저")]
    [SerializeField] private NintendoManager NintendoManager;
    private const float INPUT_DEADZONE = 0.5f;

    private InputHandlerManager m_InputHandler;
    private UIManager UIManager;
    private GameManager GameManager;
    private void Awake()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.PanelUIRegister(UIType.Stage, this);
    }

    private void Start()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_Content);

        if (m_Slot.Count > 0)
        {
            m_CurrentIndex = Mathf.Clamp(m_CurrentIndex, 0, m_Slot.Count - 1);
            SnapToIndex(m_CurrentIndex, _instant: true);
        }

        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        GameManager.m_StageClearAction += StageClear;
    }

    private void OnDestroy()
    {
        if(null != GameManager)
        {
            GameManager.m_StageClearAction -= StageClear;
        }
    }

    #region Input

    public void RegisterToInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.RegisterCallback("Player", "Move", OnMove, ActionPhaseType.Started);
        _inputHandler.RegisterCallback("Player", "Select", OnSelect, ActionPhaseType.Started);
        _inputHandler.RegisterCallback("Debug", "StageReset", OnStageReset, ActionPhaseType.Started);
    }

    public void UnregisterFromInputHandler(InputHandlerManager _inputHandler)
    {
        _inputHandler.UnregisterCallback("Player", "Move", OnMove, ActionPhaseType.Started);
        _inputHandler.UnregisterCallback("Player", "Select", OnSelect, ActionPhaseType.Started);
        _inputHandler.UnregisterCallback("Debug", "StageReset", OnStageReset, ActionPhaseType.Started);
    }

    private void OnMove(InputAction.CallbackContext _ctx)
    {
        float x = _ctx.ReadValue<Vector2>().x;

        if (Mathf.Abs(x) < INPUT_DEADZONE) return;

        MoveIndex(x > 0 ? 1 : -1);
    }

    private void OnSelect(InputAction.CallbackContext _ctx)
    {
        if (m_Slot.Count == 0) return;

        var slot = m_Slot[m_CurrentIndex];
        if (slot == null) return;
        if (true == slot.IsClear) return;

        slot.Action();
        Close();
    }

    private void OnStageReset(InputAction.CallbackContext _ctx)
    {
        NintendoManager?.ResetSaveData(StageReset);
        PlayerPrefs.Save();
    }

    private void StageReset()
    {
        foreach (var slot in m_Slot)
        {
            slot.ResetClear();
        }
    }

    #endregion

    #region Slot Logic

    private void MoveIndex(int _dir)
    {
        if (m_Slot.Count == 0) return;

        int next = Mathf.Clamp(m_CurrentIndex + _dir, 0, m_Slot.Count - 1);
        if (next == m_CurrentIndex) return;

        m_CurrentIndex = next;
        SnapToIndex(m_CurrentIndex, _instant: false);
    }

    private void SnapToIndex(int _index, bool _instant)
    {
        if (m_Slot.Count == 0) return;

        _index = Mathf.Clamp(_index, 0, m_Slot.Count - 1);

        RectTransform slot = m_Slot[_index].GetComponent<RectTransform>();
        RectTransform viewport = m_Scroll.viewport;

        // slot 중앙을 viewport 로컬 좌표로 변환
        Vector3 slotWorldCenter = slot.TransformPoint(slot.rect.center);
        Vector3 slotCenterInViewLocal = viewport.InverseTransformPoint(slotWorldCenter);

        // viewport 중앙 (pivot 고려)
        float viewCenterX = (0.5f - viewport.pivot.x) * viewport.rect.width;

        float deltaX = slotCenterInViewLocal.x - viewCenterX;

        Vector2 targetPos = m_Content.anchoredPosition;
        targetPos.x -= deltaX;

        // Content 이동 범위 Clamp
        Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, m_Content);

        float viewLeft = -viewport.rect.width * viewport.pivot.x;
        float viewRight = viewport.rect.width * (1f - viewport.pivot.x);

        if (b.size.x > viewport.rect.width)
        {
            float moveX = targetPos.x - m_Content.anchoredPosition.x;

            float newMinX = b.min.x + moveX;
            float newMaxX = b.max.x + moveX;

            if (newMinX > viewLeft)
                targetPos.x += (viewLeft - newMinX);

            if (newMaxX < viewRight)
                targetPos.x += (viewRight - newMaxX);
        }

        m_Content.DOKill();

        if (_instant)
            m_Content.anchoredPosition = targetPos;
        else
            m_Content.DOAnchorPos(targetPos, m_MoveDuration).SetEase(m_Ease);
    }

    #endregion

    public void Open()
    {
        SetChildrenActive(true);
        m_InputHandler ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        m_InputHandler.ReplaceInputReceiver(this);
    }
    public void Close() => SetChildrenActive(false);

    private void SetChildrenActive(bool isActive)
    {
        for (int i = 0; i < transform.childCount; ++i)
            transform.GetChild(i).gameObject.SetActive(isActive);
    }

    private void StageClear(StageData _stageData)
    {
        if (_stageData == null) return;

        for (int i = 0; i < m_Slot.Count; i++)
        {
            var slot = m_Slot[i];
            if (slot == null) continue;

            if (slot.StageData == _stageData)
            {
                slot.StageClear();
            }
        }
    }
}
