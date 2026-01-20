using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OperatorSlot : MonoBehaviour
{
    [Header("캐릭터 데이터(인스펙터확인용)")]
    [SerializeField] private OperatorData m_Data = null;

    [Header("연출 루트 (Layout 영향 X)")]
    [SerializeField] private RectTransform m_VisualRoot = null;

    [Header("UI")]
    [SerializeField] private Image m_Background = null;
    [SerializeField] private Image m_ClassIcon = null;
    [SerializeField] private Image m_OperatorIcon = null;
    [SerializeField] private Image m_Classborder = null;
    [SerializeField] private TextMeshProUGUI m_CostText = null;

    [Header("연출")]
    [SerializeField] private float m_SelectOffsetY = 12f;
    [SerializeField] private float m_SelectDuration = 0.2f;

    [SerializeField] private Vector2 m_OriginPos;
    private Tween   m_SelectTween;

    [Header("쿨타임 연출")]
    [SerializeField] private GameObject m_RedeploymentCooldownUI = null;
    [SerializeField] private float m_RedeploymentCurrentCooldown = 0;
    [SerializeField] private Slider m_RedeploymentCooldownSlider = null;
    [SerializeField] private TextMeshProUGUI m_RedeploymentCooldownText = null;
    private Coroutine m_RedeploymentCooldownRoutine = null;

    private static float m_OriginY = 0f;
    private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color EnabledColor = Color.white;

    private bool m_CanCreate = true;
    public bool CanCreate => m_CanCreate;

    private void OnEnable()
    {
        // 레이아웃 재정렬 후 기준점 캡처
        var parent = transform.parent as RectTransform;
        if (true == parent)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }

        m_OriginY = m_VisualRoot.anchoredPosition.y;
    }

    public void OperatorDataRegister(OperatorData _data)
    {
        if (null == _data)
        {
            return;
        }

        m_Data = _data;

        if(null != m_Data.m_Sprite)
        {
            if(null != m_OperatorIcon)
            {
                m_OperatorIcon.sprite = m_Data.m_Sprite;
            }
        }

        if(null != m_Data.m_ClassIcon)
        {
            if(null != m_ClassIcon)
            {
                m_ClassIcon.sprite = m_Data.m_ClassIcon;
            }
        }

        if(null != m_CostText)
        {
            m_CostText.SetText("${0}", m_Data.m_Cost);
        }
    }

    public void SlotActivation(int _coin)
    {
        if (null == m_Data) return;

        bool canUse = _coin >= m_Data.m_Cost;
        SetSlotVisual(canUse);
    }

    private void SetSlotVisual(bool _enabled)
    {
        Color c = _enabled ? EnabledColor : DisabledColor;

        if (m_Background) m_Background.color = c;
        if (m_Classborder) m_Classborder.color = c;
        if (m_OperatorIcon) m_OperatorIcon.color = c;
        if (m_ClassIcon) m_ClassIcon.color = c;
        if (m_CostText) m_CostText.color = c;
    }

    // 현재 슬롯일때 연출 처리용 
    public void CurrentSlot(bool _selected)
    {
        m_SelectTween?.Kill();

        float targetY = m_OriginY + (_selected ? m_SelectOffsetY : 0f);

        m_SelectTween = m_VisualRoot
            .DOAnchorPosY(targetY, m_SelectDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // TimeScale 영향 방지
    }

    public void ResetSlot()
    {
        m_Data = null;
        gameObject.SetActive(false);
        m_CanCreate = true;

        if (m_RedeploymentCooldownUI)
            m_RedeploymentCooldownUI.SetActive(false);
    }

    public void StartRedeploymentCooldown()
    {
        if (m_Data == null) 
            return;

        if (m_Data.RedeploymentMaxCooldown <= 0f) 
            return;

        m_RedeploymentCurrentCooldown = m_Data.RedeploymentMaxCooldown;

        if (m_RedeploymentCooldownUI)
            m_RedeploymentCooldownUI.SetActive(true);

        m_CanCreate = false;

        m_RedeploymentCooldownSlider.value = 0f;
        m_RedeploymentCooldownRoutine = StartCoroutine(RedeploymentCooldown(m_Data.RedeploymentMaxCooldown));
    }

    private IEnumerator RedeploymentCooldown(float _maxCooldown)
    {
        m_RedeploymentCurrentCooldown = _maxCooldown;

        if (m_RedeploymentCooldownUI)
            m_RedeploymentCooldownUI.SetActive(true);

        while (m_RedeploymentCurrentCooldown > 0f)
        {
            m_RedeploymentCurrentCooldown -= Time.deltaTime;

            float normalized =
                1f - (m_RedeploymentCurrentCooldown / _maxCooldown);

            if (null != m_RedeploymentCooldownSlider)
                m_RedeploymentCooldownSlider.value = Mathf.Clamp01(normalized);

            if (null != m_RedeploymentCooldownText)
                m_RedeploymentCooldownText.SetText("{0:0.0}", m_RedeploymentCurrentCooldown);

            yield return null;
        }

        // 종료 보정
        m_RedeploymentCurrentCooldown = 0f;

        if (m_RedeploymentCooldownSlider)
            m_RedeploymentCooldownSlider.value = 1f;

        if (m_RedeploymentCooldownUI)
            m_RedeploymentCooldownUI.SetActive(false);

        m_RedeploymentCooldownRoutine = null;
        OnRedeploymentCooldownFinished();
    }

    private void OnRedeploymentCooldownFinished()
    {
        if (m_RedeploymentCooldownUI)
            m_RedeploymentCooldownUI.SetActive(false);

        m_CanCreate = true;
    }
}
