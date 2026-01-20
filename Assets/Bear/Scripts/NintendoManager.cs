//#if UNITY_SWITCH && !UNITY_EDITOR
using nn.hid;
using System;
using static UnityEngine.InputSystem.Switch.NPad;
//#endif

using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class NintendoManager : MonoBehaviour
{
#if UNITY_SWITCH && !UNITY_EDITOR
    // ===================== SaveData =====================
    private nn.account.Uid userId;
    private const string mountName = "MySave";
    private const string fileName = "MySaveData";
    private static readonly string filePath = string.Format("{0}:/{1}", mountName, fileName);
    private nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();

    private const int saveDataVersion = 1;
    private int counter = 0;

    // ===================== Npad / ControllerSupport =====================
    private nn.hid.NpadState npadState;

    // 감시 대상(고정): 0=Handheld, 1=No1, 2=No2
    private readonly nn.hid.NpadId[] supportedNpadIds =
    {
        nn.hid.NpadId.Handheld,
        nn.hid.NpadId.No1,
        nn.hid.NpadId.No2
    };

    private ControllerSupportArg controllerSupportArg = new ControllerSupportArg();
    private nn.Result result = new nn.Result();

    // ===================== 정책: 항상 1개만 동작 =====================
    private nn.hid.NpadId activeId = nn.hid.NpadId.Invalid;

    // “새 입력” 감지용(버튼 상승 에지)
    private ulong[] lastButtons; // supportedNpadIds와 인덱스 동일

    [Header("Promote On Input")]
    [SerializeField] private float pollIntervalSeconds = 0.02f;  // 입력 감시 주기(50fps 정도)
    [SerializeField] private float showCooldownSeconds = 0.35f;  // Show 연타 방지

    private float nextPollTime = 0f;
    private float ignoreUntil = 0f;
    private bool suppress = false;

    private byte[] fileA;
    private VibrationFileInfo fileInfoA = new VibrationFileInfo();
    private VibrationFileParserContext fileContextA = new VibrationFileParserContext();

    // ===================== Unity lifecycle =====================
    private void Awake()
    {
        // ---- Account / Save Mount
        nn.account.Account.Initialize();
        nn.account.UserHandle userHandle = new nn.account.UserHandle();

        if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
            nn.Nn.Abort("Failed to open preselected user.");

        nn.Result r = nn.account.Account.GetUserId(ref userId, userHandle);
        r.abortUnlessSuccess();
        r = nn.fs.SaveData.Mount(mountName, userId);
        r.abortUnlessSuccess();

        Load();
        InitializeSaveData();

        // ---- Npad init
        Npad.Initialize();
        Npad.SetSupportedIdType(supportedNpadIds);
        NpadJoy.SetHoldType(NpadJoyHoldType.Horizontal);
        Npad.SetSupportedStyleSet(NpadStyle.FullKey | NpadStyle.Handheld | NpadStyle.JoyDual);

        npadState = new NpadState();

        // 외부 슬롯만 Single 고정(Handheld 제외)
        for (int i = 1; i < supportedNpadIds.Length; i++)
            NpadJoy.SetAssignmentModeSingle(supportedNpadIds[i]);

        // 버튼 상태 배열 준비
        lastButtons = new ulong[supportedNpadIds.Length];

        // 시작 active: 현재 연결된 것 중 하나(없으면 Invalid)
        activeId = PickFirstConnected();

        // 초기 lastButtons 스냅샷(연결 직후 눌림으로 오작동 방지)
        SnapshotAllButtons();

        InputSystem.onDeviceChange += OnDeviceChagne;
        nextPollTime = Time.unscaledTime + pollIntervalSeconds;

        ShowControllerSupportSingle();

        // 진동  System.IO.File.ReadAllBytes(Application.streamingAssetsPath + "/D05_Thumpp3.bnvib")
        fileA = System.IO.File.ReadAllBytes(Application.streamingAssetsPath + "/D05_Thumpp3.bnvib");
        result = VibrationFile.Parse(ref fileInfoA, ref fileContextA, fileA, fileA.LongLength);
        Debug.Assert(result.IsSuccess());
        VibrationFramework.Init(nn.hid.NpadId.No1, NpadStyle.FullKey);
        VibrationFramework.Init(nn.hid.NpadId.Handheld, NpadStyle.Handheld);
        VibrationFramework.StartThread();
    }

    private void OnDestroy()
    {
        nn.fs.FileSystem.Unmount(mountName);
        InputSystem.onDeviceChange -= OnDeviceChagne;
    }

    // ===================== Device change (옵션: 빠른 반응용) =====================
    // 연결/해제 이벤트가 오면 즉시 스냅샷을 갱신해서 “잘못된 상승 에지”를 줄임
    private void OnDeviceChagne(InputDevice device, InputDeviceChange change)
    {
        if (suppress) return;
        if (Time.unscaledTime < ignoreUntil) return;
        if (change != InputDeviceChange.Added && change != InputDeviceChange.Removed) return;

        // 장치 변화 직후에는 상태가 튈 수 있어서, 버튼 스냅샷만 갱신
        SnapshotAllButtons();
    }

    // ===================== 핵심: 새 입력이 들어온 디바이스가 무조건 승격 =====================
    private void Update()
    {
        if (Time.unscaledTime < nextPollTime)
            return;

        nextPollTime = Time.unscaledTime + pollIntervalSeconds;

        if (suppress) return;
        if (Time.unscaledTime < ignoreUntil) return;

        // 1) 세 디바이스의 현재 버튼을 읽고 “상승 에지(0 -> 눌림)”를 찾는다.
        nn.hid.NpadId promoted = nn.hid.NpadId.Invalid;

        for (int i = 0; i < supportedNpadIds.Length; i++)
        {
            var id = supportedNpadIds[i];

            var style = ResolveSingleStyle(id);
            if (style == nn.hid.NpadStyle.None)
                continue;

            nn.hid.Npad.GetState(ref npadState, id, style);

            ulong cur = (ulong)npadState.buttons;     // NpadButton이 ulong/bitmask로 캐스팅 가능하다는 전제
            ulong prev = lastButtons[i];

            // “새 입력”: 이전엔 0이었는데 지금 0이 아니면(처음 눌림)
            bool rising = (prev == 0UL) && (cur != 0UL);

            // 다음 프레임 비교를 위해 저장
            lastButtons[i] = cur;

            if (!rising)
                continue;

            // activeId에서 입력이 들어온 건 “승격” 의미 없으니 패스
            if (id == activeId)
                continue;

            promoted = id;
            break; // 가장 먼저 감지된 1개만
        }

        if (promoted == nn.hid.NpadId.Invalid)
            return;

        // 2) 요구사항: Show 먼저 -> 그 다음 active 교체
        PromoteTo(promoted);
    }

    private void PromoteTo(nn.hid.NpadId newActive)
    {
        // 쿨다운
        ignoreUntil = Time.unscaledTime + showCooldownSeconds;

        suppress = true;
        try
        {
            ShowControllerSupportSingle();
            activeId = newActive;

            // 승격 직후: 버튼 상태를 다시 스냅샷해서
            // “누르고 있던 버튼 때문에 연속 승격”되는 걸 방지
            SnapshotAllButtons();
        }
        finally
        {
            suppress = false;
        }
    }

    // ===================== 버튼 스냅샷 =====================
    private void SnapshotAllButtons()
    {
        for (int i = 0; i < supportedNpadIds.Length; i++)
        {
            var id = supportedNpadIds[i];

            var style = ResolveSingleStyle(id);
            if (style == nn.hid.NpadStyle.None)
            {
                lastButtons[i] = 0UL;
                continue;
            }

            nn.hid.Npad.GetState(ref npadState, id, style);
            lastButtons[i] = (ulong)npadState.buttons;
        }
    }

    // ===================== 초기 active 선택 =====================
    private nn.hid.NpadId PickFirstConnected()
    {
        // 시작 정책: Handheld 먼저(본체 플레이 기본) -> No1 -> No2
        // 원하면 No1 우선으로 바꿔도 됨.
        if (nn.hid.Npad.GetStyleSet(nn.hid.NpadId.Handheld) != nn.hid.NpadStyle.None) return nn.hid.NpadId.Handheld;
        if (nn.hid.Npad.GetStyleSet(nn.hid.NpadId.No1) != nn.hid.NpadStyle.None) return nn.hid.NpadId.No1;
        if (nn.hid.Npad.GetStyleSet(nn.hid.NpadId.No2) != nn.hid.NpadStyle.None) return nn.hid.NpadId.No2;
        return nn.hid.NpadId.Invalid;
    }

    // ===================== 스타일 정규화 (중요) =====================
    private nn.hid.NpadStyle ResolveSingleStyle(nn.hid.NpadId id)
    {
        var set = nn.hid.Npad.GetStyleSet(id);

        if ((set & nn.hid.NpadStyle.FullKey) != 0) return nn.hid.NpadStyle.FullKey;
        if ((set & nn.hid.NpadStyle.Handheld) != 0) return nn.hid.NpadStyle.Handheld;
        if ((set & nn.hid.NpadStyle.JoyDual) != 0) return nn.hid.NpadStyle.JoyDual;
        if ((set & nn.hid.NpadStyle.JoyLeft) != 0) return nn.hid.NpadStyle.JoyLeft;
        if ((set & nn.hid.NpadStyle.JoyRight) != 0) return nn.hid.NpadStyle.JoyRight;

        return nn.hid.NpadStyle.None;
    }

    // ===================== ControllerSupport (항상 1개만) =====================
    private void ShowControllerSupportSingle()
    {
        controllerSupportArg.SetDefault();
        controllerSupportArg.enableSingleMode = true;
        controllerSupportArg.playerCountMax = 1;

        controllerSupportArg.enableExplainText = true;
        ControllerSupport.SetExplainText(ref controllerSupportArg, "P1", nn.hid.NpadId.No1);

        result = ControllerSupport.Show(controllerSupportArg);
        if (!result.IsSuccess())
            Debug.LogError(result);
    }

    // ===================== SaveData Load =====================
    private void Load()
    {
        nn.fs.EntryType entryType = 0;
        nn.Result r = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(r))
            return;

        r.abortUnlessSuccess();

        r = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Read);
        r.abortUnlessSuccess();

        long fileSize = 0;
        r = nn.fs.File.GetSize(ref fileSize, fileHandle);
        r.abortUnlessSuccess();

        byte[] data = new byte[fileSize];
        r = nn.fs.File.Read(fileHandle, 0, data, fileSize);
        r.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        UnityEngine.Switch.PlayerPrefsHelper.rawData = data;

        using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
        {
            int version = reader.ReadInt32();
            Debug.Assert(version == saveDataVersion);
            counter = reader.ReadInt32();
        }
    }

    private void SavePlayerPrefs()
    {
        PlayerPrefs.Save();
        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;
        long saveDataSize = data.LongLength;

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        try
        {
            // 파일 없으면 생성
            nn.fs.EntryType entryType = 0;
            var r0 = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
            if (nn.fs.FileSystem.ResultPathNotFound.Includes(r0))
            {
                var rc = nn.fs.File.Create(filePath, saveDataSize);
                rc.abortUnlessSuccess();
            }
            else
            {
                r0.abortUnlessSuccess();
            }

            nn.Result r = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
            r.abortUnlessSuccess();

            //  핵심: 파일 크기 조정(늘어나도 OK)
            r = nn.fs.File.SetSize(fileHandle, saveDataSize);
            r.abortUnlessSuccess();

            r = nn.fs.File.Write(fileHandle, 0, data, saveDataSize, nn.fs.WriteOption.Flush);
            r.abortUnlessSuccess();

            nn.fs.File.Close(fileHandle);

            r = nn.fs.FileSystem.Commit(mountName);
            r.abortUnlessSuccess();
        }
        finally
        {
            UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
        }
    }

    private void InitializeSaveData()
    {
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (result.IsSuccess())
        {
            return;
        }
        if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            result.abortUnlessSuccess();
        }

        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;
        long saveDataSize = data.LongLength;

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        result = nn.fs.File.Create(filePath, saveDataSize);
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        const int offset = 0;
        result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
    }
#endif

    public void ResetSaveData(Action _action)
    {
        Vibration();
        if (null != _action)
        {
            _action.Invoke();
        }

#if UNITY_SWITCH && !UNITY_EDITOR
        SavePlayerPrefs();
#endif
    }

    public void FlushPlayerPrefsToSaveData()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        SavePlayerPrefs();
#endif
    }

    public void Vibration()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        VibrationFramework.Play(activeId, VibrationFramework.HidVibrationSlot.Slot1, fileA);
#endif
    }

}



