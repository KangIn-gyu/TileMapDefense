using UnityEngine;

public class GameplayUI : MonoBehaviour, IPanelUI
{
    [Header("자식오브젝트에서 참조")]
    [SerializeField] private ArrowUI m_ArrowUI = null;
    [SerializeField] private AttackDirUI m_AttackDirUI = null;

    /// <summary>
    /// 해당 오브젝트는 무조건 활성화 상태여야 한다. 
    /// 비활성화 상태면 Awake를 실행을 안해서
    /// 게임 매니저에서 UI 세팅 함수가 실행이 안됨.
    /// </summary>
    public void Awake()
    {
        ManagerHub.Instance.GetManager<UIManager>().PanelUIRegister(UIType.GamePlayUI, this);
    }

    public void Open()
    {
        SetChildrenActive(true);
        // 추후 키 바인딩 요청
    }
    public void Close()
    {
        SetChildrenActive(false);
        // 추후 키 바인딩 요청
    }

    private void SetChildrenActive(bool isActive)
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(isActive);
        }

        m_ArrowUI.Awake();
        m_ArrowUI.Close();

        m_AttackDirUI.Awake();
        m_AttackDirUI.Close();
    }
}
