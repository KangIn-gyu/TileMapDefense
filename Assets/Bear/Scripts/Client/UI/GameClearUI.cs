using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameClearUI : MonoBehaviour, IActionUI
{
    [Header("자식오브젝트 참조")]
    [SerializeField] private Image m_Background = null;
    [SerializeField] private GameObject m_GameClearUI = null;

    [Header("연출")]
    [SerializeField] private float m_BackgroundAlpha = 180f;

    private UIManager UIManager = null;

    private void Awake()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.ActionUIRegister(UIType.GameClear, this);
    }

    public void Action()
    {
        m_Background.gameObject.SetActive(true);
        m_Background.color = new Color(0f, 0f, 0f, 0f);

        RectTransform rect = m_GameClearUI.GetComponent<RectTransform>();

        Vector2 targetPos = rect.anchoredPosition;
        Vector2 startPos = targetPos + Vector2.left * 800f; // 화면 왼쪽

        rect.anchoredPosition = startPos;
        m_GameClearUI.SetActive(true);

        // DOTween 시퀀스
        Sequence seq = DOTween.Sequence();

        seq.Join(
            m_Background
                .DOFade(m_BackgroundAlpha / 255f, 0.6f)
                .SetEase(Ease.OutQuad)
        );

        seq.Join(
            rect
                .DOAnchorPos(targetPos, 0.6f)
                .SetEase(Ease.OutBack)
        ).OnComplete(() => { Invoke("Cancel", 1f); });
    }

    public void Cancel() 
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.SetPanelActive(UIType.Stage, _isActive : true);
        UIManager.SetPanelActive(UIType.GamePlayUI, _isActive: false);
    }

    public void Init()
    {

    }
}
