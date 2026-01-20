using TMPro;
using UnityEngine;

public class LifeUI : MonoBehaviour, IValueUI<int>
{
    [Header("자식 오브젝트 참조")]
    [SerializeField] private TextMeshProUGUI m_TextMeshPro;

    private const int DangerThreshold = 3;
    private const int WarningThreshold = 5;

    private readonly Color ColorDanger = Color.red;
    private readonly Color ColorWarning = new Color(1f, 0.65f, 0f); // 주황
    private readonly Color ColorNormal = Color.white;

    private void Awake()
    {
        ManagerHub.Instance.GetManager<UIManager>().ValueUIRegister(UIType.Life,this);
    }

    public void OnValueChanged(int _value)
    {
        if (_value < DangerThreshold)
        {
            m_TextMeshPro.color = ColorDanger;
        }
        else if (_value < WarningThreshold)
        {
            m_TextMeshPro.color = ColorWarning;
        }
        else
        {
            m_TextMeshPro.color = ColorNormal;
        }

        m_TextMeshPro.SetText("{0}", _value);
    }

}
