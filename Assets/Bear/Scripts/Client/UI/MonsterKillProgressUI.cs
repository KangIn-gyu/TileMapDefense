using TMPro;
using UnityEngine;

public class MonsterKillProgressUI : MonoBehaviour, IValueUI<int>
{
    [Header("자식오브젝트에서 참조")]
    [SerializeField] private TextMeshProUGUI m_MonsterKillProgressUIText = null;

    private int m_MAXMonsterKillProgress = 0;  
    private GameManager GameManager = null;  

    private void Awake()
    {
        ManagerHub.Instance.GetManager<UIManager>().ValueUIRegister(UIType.MonsterKillProgressUI, this);
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
    }

    public void OnValueChanged(int _value)
    {
        m_MAXMonsterKillProgress = GameManager.MaxMonsterKillCount();
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        m_MonsterKillProgressUIText.SetText("{0} / {1}", _value, m_MAXMonsterKillProgress);
    }
}
