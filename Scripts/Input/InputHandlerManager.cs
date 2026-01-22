using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum ActionPhaseType
{
    Started,
    Performed,
    Canceled
}

public class InputHandlerManager : MonoBehaviour
{
    public BaseActions m_BaseActions = null;
    private IInputReceiver m_InputReceiver = null;

    private void Awake()
    {
        if(null == m_BaseActions)
        {
            m_BaseActions = new BaseActions();
        }
    }

    private void OnEnable()
    {
        m_BaseActions.Enable();
    }

    private void OnDisable()
    {
        m_BaseActions.Disable();
    }

    public void ReplaceInputReceiver(IInputReceiver _IInputReceiver)
    {
        if (null != m_InputReceiver)
        {
            m_InputReceiver.UnregisterFromInputHandler(this);
        }

        m_InputReceiver = _IInputReceiver;
        m_InputReceiver.RegisterToInputHandler(this);
    }

    // 아무 키를 눌렀을때 체크용 
    public bool IsAnyInputDown()
    {
        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void RegisterCallback(string _actionMapName, string _actionName, Action<InputAction.CallbackContext> _callback, ActionPhaseType _Type)
    {
        ModifyCallback(_actionMapName, _actionName, _callback, _Type, true);
    }

    public void UnregisterCallback(string _actionMapName, string _actionName, Action<InputAction.CallbackContext> _callback, ActionPhaseType _Type)
    {
        ModifyCallback(_actionMapName, _actionName, _callback, _Type, false);
    }

    private void ModifyCallback(string _actionMapName, string _actionName, Action<InputAction.CallbackContext> _callback, ActionPhaseType _phaseType, bool _shouldRegister)
    {
        if (true == string.IsNullOrEmpty(_actionName) || _callback == null)
        {
            return;
        }

        if (null == m_BaseActions)
        {
            m_BaseActions = new BaseActions();
        }

        var actionMap = m_BaseActions.asset.FindActionMap(_actionMapName);
        if (null == actionMap)
        {
            DebugUtility.LogMessage(LogType.Warning, $"[에러] '{_actionMapName}'에 해당하는 InputAction을 찾을 수 없습니다.");
            return;
        }

        var action = m_BaseActions.FindAction(_actionName);
        if (null == action)
        {
            DebugUtility.LogMessage(LogType.Warning, $"[에러] '{_actionName}'에 해당하는 InputAction을 찾을 수 없습니다.");
            return;
        }

        switch (_phaseType)
        {
            case ActionPhaseType.Started:
                if (true == _shouldRegister)
                {
                    action.started += _callback;
                }
                else
                {
                    action.started -= _callback;
                }
                break;

            case ActionPhaseType.Performed:
                if (true == _shouldRegister)
                {
                    action.performed += _callback;
                }
                else
                {
                    action.performed -= _callback;
                }
                break;

            case ActionPhaseType.Canceled:
                if (true == _shouldRegister)
                {
                    action.canceled += _callback;
                }
                else
                {
                    action.canceled -= _callback;
                }
                break;
        }
    }

    public bool IsAnyInputDown(float axisEpsilon = 0.15f)
    {
        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                // 버튼류(키보드/패드 버튼 등)
                if (control is ButtonControl b && b.wasPressedThisFrame)
                    return true;

                // 스틱/트리거/터치/마우스 델타 등 값 계열 포함
                if (control.IsActuated(axisEpsilon))
                    return true;
            }
        }
        return false;
    }
}
