using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

public static class DebugUtility
{
    // 참조가 할당돼 있는지 점검
    public static void CheckNull(string _objName, Component _source, string _message = null,
           [CallerMemberName] string _caller = "",
           [CallerFilePath] string _file = "",
           [CallerLineNumber] int _line = 0)
    {
#if UNITY_EDITOR
        if (null == _source)
        {
            string fileName = System.IO.Path.GetFileName(_file);
            string objDisplayName = (_source != null) ? _source.gameObject.name : "Unknown";
            string errMsg = _message ?? $"[{objDisplayName}] ({fileName}:{_line}) {_caller}() 안에서 {_objName}가 null 입니다.";
            Debug.LogError(errMsg);
        }
#endif
    }

    public static void CheckGameObject(string _label, GameObject _obj,
          [CallerMemberName] string _caller = "",
          [CallerFilePath] string _file = "",
          [CallerLineNumber] int _line = 0)
    {
#if UNITY_EDITOR
        if (null == _obj)
        {
            string fileName = System.IO.Path.GetFileName(_file);
            Debug.LogError($"[{fileName}:{_line}] {_caller}()에서 {_label} GameObject가 존재하지 않습니다.");
        }
#endif
    }
    
    // 특정 오브젝트에 컴포넌트 존재 여부 체크 (AddComponent 누락 등)
    public static void CheckComponent<T>(GameObject _obj,
           [CallerMemberName] string _caller = "",
           [CallerFilePath] string _file = "",
           [CallerLineNumber] int _line = 0) where T : Component
    {
#if UNITY_EDITOR
        if (null == _obj || null == _obj.GetComponent<T>())
        {
            string fileName = System.IO.Path.GetFileName(_file);
            Debug.LogError($"[{fileName}:{_line}] {_caller}()에서 {_obj?.name ?? "Unknown"}에 {typeof(T).Name} 컴포넌트가 없습니다.");
        }
#endif
    }

    public static void LogMessage(LogType _log, string _message)
    {
#if UNITY_EDITOR
        switch (_log)
        {
            case LogType.Log:
                Debug.Log(_message);
                break;

            case LogType.Warning:
                Debug.LogWarning(_message);
                break;

            case LogType.Error:
                Debug.LogError(_message);
                break;
        }
#endif
    }
    public static void LogEvent(UnityEvent _unityEvent, string _label = "Event Log")
    {
#if UNITY_EDITOR
        Debug.Log($"==== {_label} ====");
        int count = _unityEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            var target = _unityEvent.GetPersistentTarget(i);
            var methodName = _unityEvent.GetPersistentMethodName(i);
            Debug.Log($"[{i}] 타겟 : {target}, 함수 : {methodName}");
        }
        Debug.Log("==== Runtime listeners 접근 제한됨 ====");
#endif
    }

      public static void LogEvent(UnityEvent2 _unityEvent, string _label = "Event Log")
      {
  #if UNITY_EDITOR
          Debug.Log($"==== {_label} ====");
          int count = _unityEvent.GetPersistentEventCount();
          for (int i = 0; i < count; i++)
          {
              var target = _unityEvent.GetPersistentTarget(i);
              var methodName = _unityEvent.GetPersistentMethodName(i);
              Debug.Log($"[{i}] 타겟 : {target}, 함수 : {methodName}");
          }
          Debug.Log("==== Runtime listeners 접근 제한됨 ====");
  #endif
      }
}