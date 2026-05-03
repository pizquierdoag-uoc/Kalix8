using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public enum LayerMode { Tiled, SingleObject }

    [Header("Modo de scroll")]
    public LayerMode mode = LayerMode.Tiled;

    [Header("Parallax")]
    [Range(0f, 2f)] public float speedMultiplier = 0.5f;

    [Header("Tiled — ancho de un tile en unidades Unity")]
    public float tileWidth = 20f;

    [Tooltip("Solapamiento entre tiles para eliminar el seam visible (0.02–0.1 suele bastar)")]
    [Range(0f, 0.5f)] public float tileOverlap = 0.05f;

    [Header("SingleObject — X de reaparición")]
    public float respawnX = 25f;

    float     _baseSpeed;
    float     _scrollOffset;
    Transform _tileA;
    Transform _tileB;

    void Awake()
    {
        if (mode == LayerMode.Tiled)
            InitTiles();
    }

    void InitTiles()
    {
        if (transform.childCount < 2)
        {
            Debug.LogError($"[ParallaxLayer] '{name}' modo Tiled requiere 2 hijos (TileA y TileB).");
            enabled = false;
            return;
        }
        _tileA = transform.GetChild(0);
        _tileB = transform.GetChild(1);
        _tileA.localPosition = Vector3.zero;
        _tileB.localPosition = new Vector3(tileWidth, 0f, 0f);
    }

    void Update()
    {
        float delta = _baseSpeed * speedMultiplier * Time.deltaTime;
        if (delta == 0f) return;

        if (mode == LayerMode.Tiled) ScrollTiled(delta);
        else                         ScrollSingle(delta);
    }

    // Mueve cada tile en espacio local; el padre permanece estático.
    void ScrollTiled(float delta)
    {
        _scrollOffset = (_scrollOffset + delta) % tileWidth;

        Vector3 a = _tileA.localPosition;
        Vector3 b = _tileB.localPosition;
        a.x = -_scrollOffset;
        b.x = tileWidth - tileOverlap - _scrollOffset;
        _tileA.localPosition = a;
        _tileB.localPosition = b;
    }

    void ScrollSingle(float delta)
    {
        transform.Translate(Vector3.left * delta, Space.World);
        if (transform.position.x < -respawnX)
        {
            Vector3 p = transform.position;
            p.x = respawnX;
            transform.position = p;
        }
    }

    // Llamado por ScrollManager en cada frame.
    public void SetBaseSpeed(float speed) => _baseSpeed = speed;

    public void Pause()  => enabled = false;
    public void Resume() => enabled = true;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (mode != LayerMode.Tiled) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Vector3 c = transform.position;
        Gizmos.DrawWireCube(c + Vector3.right * tileWidth * 0.5f,
            new Vector3(tileWidth, 11f, 0f));
        Gizmos.DrawWireCube(c + Vector3.right * tileWidth * 1.5f,
            new Vector3(tileWidth, 11f, 0f));
    }
#endif
}
