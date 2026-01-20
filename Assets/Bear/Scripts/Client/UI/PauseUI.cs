using UnityEngine;
using UnityEngine.UI;
public class PauseUI : MonoBehaviour, IActionUI
{
    [Header("자식 오브젝트 참조")]
    [SerializeField] private Button m_PauseButton;

    private GameManager GameManager = null;

    private void Awake()
    {
        GameManager = ManagerHub.Instance.GetManager<GameManager>();
        ManagerHub.Instance.GetManager<UIManager>().ActionUIRegister(UIType.Pause, this);
    }

    private void OnEnable()
    {
        if (null != m_PauseButton)
            m_PauseButton.onClick.AddListener(Action);
    }

    private void OnDisable()
    {
        if (null != m_PauseButton)
            m_PauseButton.onClick.RemoveListener(Action);
    }

    public void Action()
    {
        if (null == GameManager)
        {
            GameManager = ManagerHub.Instance.GetManager<GameManager>();
        }

        GameManager.TogglePause();
        // TODO : 추가 버튼 이미지 변경 관련해서 만들어야 됨.
    }

    public void Cancel() {  }

    public void Init()
    {

    }
}
