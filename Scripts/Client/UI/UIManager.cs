using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 필요한 UI
/// 1. 적 카운터 -> 문자열 변화
/// 2. 라이프  -> 문자열 변화
/// 3. 정지  -> 문자고정
/// 4. 배속 -> 문자 고정
/// 5. 오퍼레이터 -> 고정 UI
/// 6. 코스트 -> 슬라이더 문자열 변화
/// </summary>

public enum UIType
{
    Life,                   // 플레이어 라이프
    Cost,                   // 코스트 (슬라이더 + 텍스트)
    Pause,                  // 게임 정지 토글
    GameSpeed,              // 배속 토글 (1x / 2x)
    Operator,               // 선택된 오퍼레이터 정보
    MonsterKillProgressUI,  // 몬스터 킬 표시 UI

    GamePlayUI,
    Stage,
    GameOver,
    GameClear
}

public class UIManager : MonoBehaviour
{
    private Dictionary<UIType, IPanelUI> m_PanelUIDictionary = new();
    private Dictionary<UIType, IValueUI<int>> m_ValueUIDictionary = new();
    private Dictionary<UIType, IActionUI> m_ActionUIDictionary = new();

    private ArrowUI m_FieldArrowUI = null;     // 게임에서 한개만 필요하기 때문에 추가로 만들지 않음.
    public ArrowUI FieldArrowUI
    {
        get { return m_FieldArrowUI; }
        set { m_FieldArrowUI = value; }
    }
    private AttackDirUI m_AttackDirUI = null;  // 게임에서 한개만 필요하기 때문에 추가로 만들지 않음.
    public AttackDirUI AttackDirUI
    {
        get { return m_AttackDirUI; }
        set { m_AttackDirUI = value; }
    }


    #region 등록  
    public void PanelUIRegister(UIType _key , IPanelUI _component)
    {
        if(false == m_PanelUIDictionary.ContainsKey(_key))
        {
            m_PanelUIDictionary[_key] = _component;
        }
    }

    public void ValueUIRegister(UIType _key, IValueUI<int> _component)
    {
        if (false == m_ValueUIDictionary.ContainsKey(_key))
        {
            m_ValueUIDictionary[_key] = _component;
        }
    }

    public void ActionUIRegister(UIType _key, IActionUI _component)
    {
        if (false == m_ActionUIDictionary.ContainsKey(_key))
        {
            m_ActionUIDictionary[_key] = _component;
        }
    }
#endregion

#region 요청 
    public void SetPanelActive(UIType _key, bool _isActive)
    {
        if (true == m_PanelUIDictionary.ContainsKey(_key))
        {
            if(true == _isActive)
            {
                m_PanelUIDictionary[_key].Open();
            }
            else
            {
                m_PanelUIDictionary[_key].Close();
            }
        }
    }
    public void OnValueChanged(UIType _key, int _value)
    {
        if (true == m_ValueUIDictionary.ContainsKey(_key))
        {
            m_ValueUIDictionary[_key].OnValueChanged(_value);
        }
    }

    public void ActionUIExecution(UIType _key)
    {
        if (true == m_ActionUIDictionary.ContainsKey(_key))
        {
            m_ActionUIDictionary[_key].Action();
        }
    }

    public void ActionUICancel(UIType _key)
    {
        if (true == m_ActionUIDictionary.ContainsKey(_key))
        {
            m_ActionUIDictionary[_key].Cancel();
        }
    }

    public void InitUI(UIType _key)
    {
        if (true == m_ActionUIDictionary.ContainsKey(_key))
        {
            m_ActionUIDictionary[_key].Init();
        }
    }

    #endregion
}
