using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    [Header("Hierarchy에서 참조")]
    [SerializeField] private TileMap2D m_TileMap2D;
    private Camera m_Camera;

    private float m_wDelta = 0.4f;
    private float m_hDelta = 0.6f;

    [Header("스카이박스(스테이지 뒤에 배경) 만들기용")]
    [SerializeField] private Sprite m_SkyBox = null;
    private GameObject m_SkyBoxObject = null;
    private SpriteRenderer m_SpriteRenderer = null;
    private void Awake()
    {
        if (m_Camera == null)
        {
            m_Camera = GetComponent<Camera>();
        }
    }

    public void SetupCamera(Tilemap _tilemap, int _width, int _height)
    {
        if(null == _tilemap)
        {
            DebugUtility.LogMessage(LogType.Log, "타일맵이 존재하지 않습니다.");
        }

        // 1) 중심 좌표 계산
        Vector3Int origin = _tilemap.origin;
        Vector3Int centerCell = origin + new Vector3Int(_width / 2, _height / 2, 0);
        Vector3 centerWorld = _tilemap.CellToWorld(centerCell) + _tilemap.cellSize * 0.5f;

        // 2) OrthographicSize 계산
        float size = Mathf.Max(_width * m_wDelta, _height * m_hDelta);

        if(null == m_Camera)
        {
            m_Camera = GetComponent<Camera>();
        }
        m_Camera.orthographicSize = size;

        // 3) 카메라 위치를 중심에 맞추기
        Vector3 pos = centerWorld;
        pos.z = -10f;
        transform.position = pos;

        if(null == m_SkyBoxObject)
        {
            m_SkyBoxObject = new GameObject("Background");
            m_SpriteRenderer = m_SkyBoxObject.AddComponent<SpriteRenderer>();
        }

        UpdateBackground();
    }

    private void UpdateBackground()
    {
        if (null == m_SpriteRenderer) return;
        if (null == m_SkyBox) return;

        m_SpriteRenderer.sprite = m_SkyBox;

        float camH = m_Camera.orthographicSize * 2f;
        float camW = camH * m_Camera.aspect;

        float sW = m_SpriteRenderer.sprite.bounds.size.x;
        float sH = m_SpriteRenderer.sprite.bounds.size.y;

        float scale = Mathf.Max(camW / sW, camH / sH);
        m_SkyBoxObject.transform.localScale = Vector3.one * scale;

        Vector3 bgPos = transform.position;
        bgPos.z = 1f;
        m_SkyBoxObject.transform.position = bgPos;
    }
}
