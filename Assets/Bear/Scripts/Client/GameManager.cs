using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스테이지를 시작했을때 
/// 스크립터블 오브젝트로 데이터를 받아와서 처리하게 해야 된다.
/// </summary>

public struct GameState
{
    public int m_Life;                    // 현재 라이프
    public int m_MaxDeployableCharacters; // 스테이지에 배치 가능한 캐릭터 최대수
    public int m_Coin;                    // 게임 플레이 유닛을 사는 돈 역할을 한다.
    public int m_MonsterKillCount;        // 몬스터 카운트
    public int m_MonsterMAXKillCount;     // 몬스터 최종킬수
    public int m_DeployableCharacters;    // 배치 카운터?
}

public class GameManager : MonoBehaviour
{
    [Header("게임 스피드 인스펙터에서 건들지 마시오.")]
    [SerializeField] private GameState m_GameState;
    public int GetCoin() => m_GameState.m_Coin;

    public enum GameSpeed
    {
        Pause = 0,
        Slow,         // 0.5x
        Normal,       // 1.0x
        Fast          // 2.0x
    }
    private GameSpeed m_CurrentSpeed = GameSpeed.Normal;   // 현재 스피드
    private GameSpeed m_BeforeSpeed = GameSpeed.Normal;    // 이전 스피드

    public Action            m_UnitRemovalEvent;   // 적, 오퍼레이터 제거 이벤트
    public Action<StageData> m_StageClearAction;   // 슬롯 매니저 StageClear 저장용

    private const int m_StartingCoin = 10;

    private UIManager           UIManager = null;
    private TileMap2D           TileMap = null;
    private FieldManager        FieldManager = null;
    private OperatorSlotManager OperatorSlotManager = null;
    private MonsterSpawnManager MonsterSpawnManager = null;
    private InputHandlerManager InputHandlerManager = null;

    private StageData m_CurrentStageData = null;
    private NintendoManager NintendoManager = null;

    private void Start()
    {
        UIManager = ManagerHub.Instance.GetManager<UIManager>();
        TileMap = ManagerHub.Instance.GetManager<TileMap2D>();
        FieldManager = ManagerHub.Instance.GetManager<FieldManager>();
        OperatorSlotManager = ManagerHub.Instance.GetManager<OperatorSlotManager>();
        InputHandlerManager = ManagerHub.Instance.GetManager<InputHandlerManager>();
    }

    private float GetTimeScale(GameSpeed _speed)
    {
        m_CurrentSpeed = _speed;

        return _speed switch
        {
            GameSpeed.Pause => 0f,
            GameSpeed.Slow => 0.2f,
            GameSpeed.Normal => 1f,
            GameSpeed.Fast => 2f,
            _ => 1f
        };
    }

    private void SetGameSpeed(GameSpeed _speed)
    {
        m_BeforeSpeed = m_CurrentSpeed;
        m_CurrentSpeed = _speed;
        Time.timeScale = GetTimeScale(_speed);
    }

    // 게임 정지 버튼
    public void TogglePause()
    {
        if (GameSpeed.Pause == m_CurrentSpeed)
        {
            SetGameSpeed(GameSpeed.Normal);
        }
        else
        {
            SetGameSpeed(GameSpeed.Pause);
        }
    }

    // 배속 
    public void ToggleFast()
    {
        if (GameSpeed.Fast == m_CurrentSpeed)
        {
            SetGameSpeed(GameSpeed.Normal);
        }
        else
        {
            SetGameSpeed(GameSpeed.Fast);
        }
    }

    // 슬로우
    public void SlowGameSpeed()
    {
        SetGameSpeed(GameSpeed.Slow);
    }

    public void RestoreGameSpeed()
    {
        SetGameSpeed(m_BeforeSpeed);
    }


    #region Coin 관련 함수
    public bool TryIncreaseCoin()
    {
        if (m_GameState.m_Coin >= 99)
            return false;

        m_GameState.m_Coin++;
        return true;
    }

    public bool UseCoin(int _amount)
    {
        if (m_GameState.m_Coin < _amount)
        {
            return false;
        }

        m_GameState.m_Coin -= _amount;
        // 여기 사용 사운드 추가하면 좋음.
        return true;
    }
#endregion

    public void InGameStart(StageData _stageData)
    {
        m_CurrentStageData = _stageData;
        // 시작 코인 설정
        m_GameState.m_Coin = m_StartingCoin;
        m_GameState.m_MonsterKillCount = 0;
        m_GameState.m_MonsterMAXKillCount = _stageData.GetTotalSpawnCount();

        UISetting(_stageData);

        // 게임 스피드
        SetGameSpeed(GameSpeed.Normal);

        // 타일맵 세팅
        if(null != TileMap)
        {
            TileMap.TileMapSettings(_stageData);
        }

        if(null == MonsterSpawnManager)
        {
            MonsterSpawnManager = ManagerHub.Instance.GetManager<MonsterSpawnManager>();
        }

        // 필드 매니저 시작
        FieldManager.Init(_stageData);

        // 모든 스포너 소환 실행
        MonsterSpawnManager.SummoningBegins();
    }

    public void RePlay()
    {
        if(null != m_CurrentStageData)
        {
            InGameStart(m_CurrentStageData);
        }
    }

    private void UISetting(StageData _stageData)
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        if (null == UIManager)
        {
            return;
        }

        // 게임 플레이 판넬 
        UIManager.SetPanelActive(UIType.GamePlayUI, true);

        // 라이프
        m_GameState.m_Life = _stageData.m_PlayerLife;
        UIManager.OnValueChanged(UIType.Life, m_GameState.m_Life);

        // 슬롯 매니저 초기화
        m_GameState.m_MaxDeployableCharacters = _stageData.m_MaxDeployableCharacters;
        if(null == OperatorSlotManager)
        {
            OperatorSlotManager = ManagerHub.Instance.GetManager<OperatorSlotManager>();
        }

        int stageOperatorCount = _stageData.m_StageOperators.Count;
        for (int i = 0; i < stageOperatorCount; i++)
        {
            if (null == _stageData.m_StageOperators[i])
            {
                DebugUtility.LogMessage(LogType.Warning, $"UISetting(StageOperators) {i} 인덱스가 null입니다.");
            }
        }
        OperatorSlotManager.Init(_stageData.m_StageOperators);

        // 코스트 설정 
        UIManager.ActionUIExecution(UIType.Cost);

        // 배속 UI 초기화
        UIManager.InitUI(UIType.GameSpeed);

        // 몬스터 킬 처리
        UIManager.OnValueChanged(UIType.MonsterKillProgressUI, m_GameState.m_MonsterKillCount);
        InputHandlerManager.RegisterCallback("Player", "Pause", OnPause, ActionPhaseType.Performed);
        InputHandlerManager.RegisterCallback("Player", "DoubleSpeed", OnDoubleSpeed, ActionPhaseType.Performed);
    }

    public void LifeDecrease()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        m_GameState.m_Life--;
        UIManager.OnValueChanged(UIType.Life, m_GameState.m_Life);

        if (0 == m_GameState.m_Life)
        {
            GameOver();
        }
        else 
        {
            // 닌텐도 진동 코드
            NintendoManager ??= FindFirstObjectByType<NintendoManager>();
            NintendoManager.Vibration();
        }
    }

    public void GameOver()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.SetPanelActive(UIType.GameOver, _isActive: true);
        UIManager.ActionUICancel(UIType.Cost);

        SetGameSpeed(GameSpeed.Pause);
        InputHandlerManager.UnregisterCallback("Player", "Pause", OnPause, ActionPhaseType.Performed);
        InputHandlerManager.UnregisterCallback("Player", "DoubleSpeed", OnDoubleSpeed, ActionPhaseType.Performed);
    }

    public int MaxMonsterKillCount()
    {
        return m_GameState.m_MonsterMAXKillCount;
    }

    public void MonsterKill()
    {
        m_GameState.m_MonsterKillCount++;
        UIManager.OnValueChanged(UIType.MonsterKillProgressUI, m_GameState.m_MonsterKillCount);

        int monsterCount = m_GameState.m_MonsterKillCount + (m_CurrentStageData.m_PlayerLife - m_GameState.m_Life);
        if (monsterCount == m_GameState.m_MonsterMAXKillCount)
        {
            GameClear();
        }
    }

    public void GameClear()
    {
        int monsterCount = m_GameState.m_MonsterKillCount + (m_CurrentStageData.m_PlayerLife - m_GameState.m_Life);
        if (monsterCount == m_GameState.m_MonsterMAXKillCount)
        {
            UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
            UIManager.ActionUIExecution(UIType.GameClear);

            m_UnitRemovalEvent?.Invoke();
            if (null != m_CurrentStageData)
            {
                m_StageClearAction?.Invoke(m_CurrentStageData);
            }

#if UNITY_SWITCH && !UNITY_EDITOR
        NintendoManager ??= FindFirstObjectByType<NintendoManager>();
        NintendoManager.FlushPlayerPrefsToSaveData();
#endif
        }
    }

    private void OnPause(InputAction.CallbackContext _ctx)
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.ActionUIExecution(UIType.Pause);
    }

    private void OnDoubleSpeed(InputAction.CallbackContext _ctx)
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.ActionUIExecution(UIType.GameSpeed);
    }
}

