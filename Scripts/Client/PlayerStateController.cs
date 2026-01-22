using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// 플레이어의 키처리를 하는 곳
/// 상태에 따른 키 바인딩을 다르게 해야 한다.
/// </summary>
/// 
[System.Obsolete("해당 클래스 사용하지 마세요.")]
public class PlayerStateController : MonoBehaviour
{
    public enum PlayerSituation
    {
        None,
        Field,
        OperatorSlot,
        Options
    }

    [SerializeField] private PlayerSituation m_PlayerSituation = PlayerSituation.None;
    private Dictionary<PlayerSituation, IInputReceiver> m_InputReceivers = new();
    private InputHandlerManager InputHandlerManager = null;

    public void Awake()
    {
        ManagerHub.Instance.Register<PlayerStateController>(this);
        if(null == InputHandlerManager)
        {
            InputHandlerManager = ManagerHub.Instance.GetManager<InputHandlerManager>();
        }
    }

    public void RegisterInputReceiver(PlayerSituation _situation, IInputReceiver _receiver)
    {
        if (null == _receiver)
            return;

        // 기존에 있으면 덮어쓰기
        if (m_InputReceivers.ContainsKey(_situation))
        {
            m_InputReceivers[_situation] = _receiver;
        }
        else
        {
            m_InputReceivers.Add(_situation, _receiver);
        }
    }

    public void UnregisterInputReceiver(PlayerSituation _situation)
    {
        if (!m_InputReceivers.ContainsKey(_situation))
            return;

        // 현재 활성 상태라면 먼저 해제
        if (m_PlayerSituation == _situation)
        {
            m_InputReceivers[_situation]?.UnregisterFromInputHandler(InputHandlerManager);
        }

        m_InputReceivers.Remove(_situation);
    }

    public void ChangePlayerSituation(PlayerSituation _playerSituation)
    {
        m_PlayerSituation = _playerSituation;
        if (m_InputReceivers.TryGetValue(m_PlayerSituation, out var prevReceiver))
        {
            InputHandlerManager.ReplaceInputReceiver(prevReceiver);
        }
    }
}
