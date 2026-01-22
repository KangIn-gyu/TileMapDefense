using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class DoubleSpeedUI : MonoBehaviour, IActionUI
{
    [Header("자식 오브젝트 참조")]
    [SerializeField] private Button m_SpeedButton = null;
    [SerializeField] private TextMeshProUGUI m_DoubleSpeedTextMeshProUGUI = null;

    private bool m_IsDouble = false;
    private readonly string m_Double = "2 X";
    private readonly string m_Normal = "1 X";
    private GameManager GameManager = null;

    public void Awake()
    {
        GameManager = ManagerHub.Instance.GetManager<GameManager>();
        ManagerHub.Instance.GetManager<UIManager>().ActionUIRegister(UIType.GameSpeed, this);
    }

    private void OnEnable()
    {
        if (null != m_SpeedButton)
            m_SpeedButton.onClick.AddListener(Action);
    }

    private void OnDisable()
    {
        if (null != m_SpeedButton)
            m_SpeedButton.onClick.RemoveListener(Action);
    }

    public void Action()
    {
        m_IsDouble = !m_IsDouble;

        if (null == GameManager)
        {
            GameManager = ManagerHub.Instance.GetManager<GameManager>();
        }
         

        if (true == m_IsDouble)
        {
            if(null != m_DoubleSpeedTextMeshProUGUI)
            {
                m_DoubleSpeedTextMeshProUGUI.SetText(m_Double);
                GameManager.ToggleFast();
            }
        }
        else
        {
            if (null != m_DoubleSpeedTextMeshProUGUI)
            {
                m_DoubleSpeedTextMeshProUGUI.SetText(m_Normal);
                GameManager.ToggleFast();
            }
        }
    }

    public void Cancel() { }

    public void Init()
    {
        m_DoubleSpeedTextMeshProUGUI.SetText(m_Normal);
        m_IsDouble = false;
    }
}
