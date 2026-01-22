using UnityEngine;

public class AttackDirUI : MonoBehaviour, IPanelUI
{
    [Header("자식 오브젝트 참조")]
    [SerializeField] RectTransform outerRect; // 부모(큰 마름모)
    [SerializeField] RectTransform innerRect; // 자식(방향 마름모)
    [SerializeField] float maxRadius = 80f;   // 외각 마름모 한 꼭지점까지 거리(반지름 비슷한 개념)

    private RectTransform m_RectTransform = null;

    Vector2 currentDir = Vector2.zero;
    private UIManager UIManager = null;

    private Camera m_Camera = null;

    public void Awake()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        if(null != UIManager)
        {
            UIManager.AttackDirUI = this;
        }

        m_Camera ??= Camera.main;
        m_RectTransform ??= GetComponent<RectTransform>();  
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);    
    }

    public void SetDirection(Vector2 _dir)
    {
        // 입력 방향을 정규화 (0이면 그대로 둠)
        currentDir = _dir.sqrMagnitude > 0.001f ? _dir.normalized : Vector2.zero;

        // 우선 원하는 위치 계산
        Vector2 targetPos = currentDir * maxRadius;

        // 마름모 내부로 클램프
        innerRect.anchoredPosition = ClampDiamond(targetPos, maxRadius);
    }

    private Vector2 ClampDiamond(Vector2 _pos, float _a)
    {
        float ax = Mathf.Abs(_pos.x);
        float ay = Mathf.Abs(_pos.y);

        float sum = ax + ay;

        if (sum <= _a || sum < 0.0001f)
            return _pos;

        // 경계까지 스케일링
        float k = _a / sum;
        return _pos * k;
    }

    public void SetTarget(Transform _transform)
    {
        var worldPos = _transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        m_RectTransform ??= GetComponent<RectTransform>();
        m_RectTransform.position = screenPos;
    }
}
