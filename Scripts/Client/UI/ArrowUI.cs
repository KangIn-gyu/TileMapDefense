using DG.Tweening;
using UnityEngine;

public class ArrowUI : MonoBehaviour, IValueUI<Vector2Int>, IPanelUI
{
    /// <summary>
    /// 첫 방식은 ArrowUI 오브젝트 자체에 이미지도 같이 가지고 있었는데
    /// 닷트윈을 통한 위아래 반복 움직임으로 인해 슬롯 이동이 제한되어 
    /// 자식 오브젝트로 화살표 오브젝트를 관리하게 처리하였다.
    /// </summary>
    [Header("자식 오브젝트 참조")]
    [SerializeField] private GameObject m_ArrowImage = null;

    private Camera Camera = null;
    private TileMap2D TileMap = null;
    private UIManager UIManager = null;

    private Tween m_FloatTween = null;

    public void Awake()
    {
        Camera ??= Camera.main;
        Invoke("Init", Time.fixedTime);
    }

    private void Init()
    {
        TileMap ??= ManagerHub.Instance.GetManager<TileMap2D>();
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        if(null != UIManager)
        UIManager.FieldArrowUI = this;
    }

    private void Start()
    {
        Camera ??= Camera.main;
        TileMap ??= ManagerHub.Instance.GetManager<TileMap2D>();
    }

    private void OnEnable()
    {
        UIManager ??= ManagerHub.Instance.GetManager<UIManager>();
        UIManager.FieldArrowUI = this;
    }


    public void OnValueChanged(Vector2Int _value)
    {
        // 1. Cell → World
         TileMap ??= ManagerHub.Instance.GetManager<TileMap2D>();

        Vector3 worldPos = TileMap.BaseTileMap.GetCellCenterWorld(
            new Vector3Int(_value.x, _value.y, 0)
        );

        // 2. World → Screen
        Camera ??= Camera.main;
        Vector3 screenPos = Camera.WorldToScreenPoint(worldPos);

        transform.position = screenPos;
    }

    public void Open()
    {
        gameObject.SetActive(true);
        PlayFloat();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        StopFloat();
    }

    private void PlayFloat()
    {
        m_FloatTween?.Kill();

        m_FloatTween = m_ArrowImage.transform
            .DOLocalMoveY(20f, 0.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void StopFloat()
    {
        m_FloatTween?.Kill();
        m_FloatTween = null;
    }
}
