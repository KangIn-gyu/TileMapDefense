using UnityEngine;
using AYellowpaper.SerializedCollections;

public class ManagerHub : MonoBehaviour
{
    private static ManagerHub m_Instance = null;
    public static ManagerHub Instance
    {
        get
        {
            if (null == m_Instance)
            {
                m_Instance = FindAnyObjectByType<ManagerHub>();
                if(null == m_Instance)
                {
                    var gameObj = new GameObject("ManagerHub");
                    m_Instance = gameObj.AddComponent<ManagerHub>();
                    if (false == m_Instance.m_Initialized)
                    {
                        m_Instance.Initialize();
                        m_Instance.m_Initialized = true;
                    }
                }
            }
            return m_Instance;
        }
    }

    [Header("건들지 마시오. (디버그)")]
    [SerializeField] private SerializedDictionary<string, Object> m_Managers = new();

    private bool m_Initialized = false; // 초기화 체크용

    private void Awake()
    {
        if(null == m_Instance)
        {
            m_Instance = FindAnyObjectByType<ManagerHub>();
            DontDestroyOnLoad(gameObject);
            if(false == m_Initialized)
            {
                m_Initialized = true;
                Initialize();
            }
        }
        else
        {
            if(this != m_Instance)
            {
                Destroy(this);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    public void Register<T>(T _manager) where T : UnityEngine.Object
    {
        var typeName = typeof(T).FullName;
        if (true == m_Managers.ContainsKey(typeName))
        {
            DebugUtility.LogMessage(LogType.Warning, $"[ManagerHub] {typeName}은 이미 등록되어 덮어씁니다.");
        }
        m_Managers[typeName] = _manager;
    }

    public T GetManager<T>() where T : UnityEngine.Object
    {
        var typeName = typeof(T).FullName;

        if (m_Managers.TryGetValue(typeName, out var manager))
        {
            if (manager == null)
            {
                DebugUtility.LogMessage(LogType.Warning, $"[ManagerHub] {typeName}은 이미 파괴된 매니저라서 제거합니다.");
                m_Managers.Remove(typeName);
                return null;
            }

            return manager as T;
        }

        DebugUtility.LogMessage(LogType.Log, $"[ManagerHub] {typeName}컴포넌트을 가진 오브젝트가 존재하지 않습니다.");
        return null;
    }

    public void Unregister<T>() where T : UnityEngine.Object
    {
        var typeName = typeof(T).FullName;
        m_Managers.Remove(typeName);
    }

    private void CreateManager<T>() where T : MonoBehaviour
    {
        var typeName = typeof(T).FullName;

        // 이미 있으면 반환
        if (m_Managers.TryGetValue(typeName, out var existing))
        {
            DebugUtility.LogMessage(LogType.Warning, $"[ManagerHub] {typeName}은 이미 존재하여 반환합니다.");
            return;
        }

        // 새 자식 오브젝트 생성
        GameObject child = new GameObject(typeName);
        child.transform.SetParent(this.transform);

        // 매니저 컴포넌트 추가
        T manager = child.AddComponent<T>();

        // 등록
        Register(manager);
        return;
    }

    private void Initialize()
    {
        CreateManager<InputHandlerManager>();
        CreateManager<ObjectPoolManager>();
        CreateManager<GameManager>();
        CreateManager<UIManager>();
    }
}
