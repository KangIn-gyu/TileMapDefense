using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageSlotUI : MonoBehaviour, IActionUI
{
    [Header("(에셋) ScriptableObjects-> StageData 추가 필요")]
    [SerializeField] private StageData m_StageData = null;
    public StageData StageData => m_StageData;

    private GameManager GameManager = null;   
    private UIManager UIManager = null;

    [Header("디버그 건들지마시오.")]
    [SerializeField, Tooltip("스테이지 클리어 여부 확인")] private bool m_IsClear = false; 
    public bool IsClear => m_IsClear;

    private Image m_Image = null;
    private Color m_Color = new(0.39f, 0.39f, 0.39f);
    private Color m_DefaultColor = Color.white;

    private void Start()
    {
        Load();
    }

    public void Action()
    {
        GameManager ??= ManagerHub.Instance.GetManager<GameManager>();
        UIManager   ??= ManagerHub.Instance.GetManager<UIManager>();

        if (null != m_StageData)
        {
            if(true == m_IsClear)
            {
                return;
            }
            GameManager.InGameStart(m_StageData);
        }
    }

    public void Cancel() { }

    public void Init() { }


    private void Save()
    {
        PlayerPrefs.SetInt(gameObject.name, m_IsClear ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        m_IsClear = (PlayerPrefs.GetInt(gameObject.name) == 1);
        if(true == m_IsClear)
        {
            ClearUIUpdate();
        }
    }

    public void StageClear()
    {
        m_IsClear = true;
        Save();
        ClearUIUpdate();
    }

    private void ClearUIUpdate()
    {
        m_Image ??= GetComponent<Image>();
        m_Image.color = m_Color;

        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>())
        {
            tmp.color = m_Color;
        }
    }

    private void UnclearUIUpdate()
    {
        m_Image ??= GetComponent<Image>();
        m_Image.color = m_DefaultColor;

        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>())
            tmp.color = m_DefaultColor;
    }

    public void ResetClear()
    {
        m_IsClear = false;
        PlayerPrefs.SetInt(gameObject.name, 0);
        PlayerPrefs.Save();
        UnclearUIUpdate();
    }
}
