using DG.Tweening;
using TMPro;
using UnityEngine;

public class TitleUI : MonoBehaviour, IPanelUI
{
    [Header("자식 오브젝트를 참조하시오.")]
    [SerializeField] private TextMeshProUGUI m_TitleNameText = null;
    [SerializeField] private TextMeshProUGUI m_PleasepressanykeyText = null;

    [Header("연출")]
    [SerializeField] private float m_FadeTime = 0.3f;

    private InputHandlerManager InputHandlerManager = null;
    private UIManager UIManager = null;

    private Tween m_PressAnyKeyTween;
    private Tween m_TitleBounceTween;

    private bool m_PressAnyKey = false;

    private void Awake()
    {
        InputHandlerManager ??= ManagerHub.Instance.GetManager<InputHandlerManager>();
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
    }

    private void OnEnable()
    {
        PlayPressAnyKeyTween();
        PlayTitleBounceTween();
    }

    private void Update()
    {
        if(true == m_PressAnyKey)
        {
            return;
        }

        if(true == InputHandlerManager.IsAnyInputDown())
        {
            m_PressAnyKey = true;
            DOVirtual.DelayedCall(0.3f, () =>
            {
                Close();
                UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
                UIManager.SetPanelActive(UIType.Stage, _isActive : true);
            });
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
        KillTweens();
        m_PressAnyKey = false;  
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }


    #region Tween Logic

    private void PlayPressAnyKeyTween()
    {
        // 기존 트윈 정리
        m_PressAnyKeyTween?.Kill();

        if(null != m_PleasepressanykeyText)
        {
            Color color = m_PleasepressanykeyText.color;
            color.a = 180f / 255f;
            m_PleasepressanykeyText.color = color;

            m_PressAnyKeyTween = m_PleasepressanykeyText
                .DOFade(1f, m_FadeTime)                 
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    private void PlayTitleBounceTween()
    {
        m_TitleBounceTween?.Kill();

        if(null != m_TitleNameText)
        {
            m_TitleNameText.transform.localScale = Vector3.one;

            m_TitleBounceTween = m_TitleNameText.transform
                .DOPunchScale(
                    punch: new Vector3(0.15f, 0.15f, 0f),
                    duration: 1.2f,
                    vibrato: 1,
                    elasticity: 0.5f)
                .SetLoops(-1)
                .SetEase(Ease.OutBack);
        }
    }

    private void KillTweens()
    {
        m_PressAnyKeyTween?.Kill();
        m_TitleBounceTween?.Kill();
    }

    #endregion
}
