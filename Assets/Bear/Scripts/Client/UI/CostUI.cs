using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CostUI : MonoBehaviour , IActionUI
{
    [Header("자식 오브젝트 참조")]
    [SerializeField] private Slider          m_CostSlider;
    [SerializeField] private TextMeshProUGUI m_CostText;

    private float m_CostTimer = 0f;
    [SerializeField] private float m_CostIncreaseInterval = 3f;

    private bool m_Action = false;
    private GameManager m_GameManager = null;

    private const int m_MaxCost = 99;

    private void Awake()
    {
        ManagerHub.Instance.GetManager<UIManager>().ActionUIRegister(UIType.Cost, this);
        m_GameManager = ManagerHub.Instance.GetManager<GameManager>();
    }

    private void Update()
    {
        if (false == m_Action || Time.timeScale == 0)
            return;

        if (m_GameManager.GetCoin() >= m_MaxCost)
        {
            m_CostSlider.value = 1f;
            return;
        }

        m_CostTimer += Time.deltaTime;
        float progress = m_CostTimer / m_CostIncreaseInterval;
        m_CostSlider.value = progress;

        if (m_CostTimer >= m_CostIncreaseInterval)
        {
            m_CostTimer = 0f;

            if (m_GameManager.TryIncreaseCoin())
            {
                m_CostText.SetText("{0}", m_GameManager.GetCoin());
            }
        }
    }

    public void Action()
    {
        m_Action = true;
    }

    public void Cancel()
    {
        m_Action = false;
    }

    public void Init()
    {
        
    }
}
