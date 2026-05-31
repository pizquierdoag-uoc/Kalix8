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

    [Tooltip("Voltea TileB horizontalmente para eliminar el corte cuando la imagen no es seamless")]
    public bool mirrorTileB = false;

    [Header("SingleObject — X de reaparición")]
    public float respawnX = 25f;

    [Tooltip("Margen extra (unidades) que se suma a los bounds del sprite para evitar culling prematuro")]
    public float boundsMargin = 4f;

    float     _baseSpeed;
    float     _singleHalfWidth;   // mitad del ancho del sprite (calculado en Awake)
    Transform _tileA;
    Transform _tileB;

    void Awake()
    {
        if (mode == LayerMode.Tiled)
            InitTiles();
        else
            InitSingle();
    }

    void InitSingle()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // Calculamos el semiancho para usarlo en la condición de reposicionamiento
        _singleHalfWidth = sr.bounds.size.x * 0.5f;

        // Extendemos los localBounds para evitar que Unity descarte el sprite
        // por frustum culling antes de que salga visualmente de pantalla
        Bounds b = sr.localBounds;
        b.Expand(new Vector3(boundsMargin * 2f, 0f, 0f));
        sr.localBounds = b;
    }

    void InitTiles()
    {
        if (transform.childCount == 0)
        {
            Debug.LogError($"[ParallaxLayer] '{name}' modo Tiled requiere al menos 1 hijo (TileA).");
            enabled = false;
            return;
        }

        _tileA = transform.GetChild(0);

        if (transform.childCount >= 2)
        {
            _tileB = transform.GetChild(1);
        }
        else
        {
            var goB = Instantiate(_tileA.gameObject, transform);
            goB.name = _tileA.name + "_Auto";
            _tileB = goB.transform;
        }

        var srA = _tileA.GetComponent<SpriteRenderer>();
        if (srA != null && srA.sprite != null)
        {
            float actualWidth = srA.bounds.size.x;
            if (actualWidth > 0.01f && Mathf.Abs(actualWidth - tileWidth) > 0.05f)
            {
                Debug.Log($"[ParallaxLayer] '{name}': tileWidth corregido de {tileWidth} a {actualWidth} (ancho real del sprite).");
                tileWidth = actualWidth;
            }
        }

        _tileA.localPosition = Vector3.zero;
        _tileB.localPosition = new Vector3(tileWidth - tileOverlap, 0f, 0f);

        if (mirrorTileB)
        {
            var sr = _tileB.GetComponent<SpriteRenderer>();
            if (sr != null) sr.flipX = true;
        }
    }

    void Update()
    {
        float delta = _baseSpeed * speedMultiplier * Time.deltaTime;
        if (delta == 0f) return;

        if (mode == LayerMode.Tiled) ScrollTiled(delta);
        else                         ScrollSingle(delta);
    }

    void ScrollTiled(float delta)
    {
        // Scroll independiente por tile + teleport al cruzar el umbral izquierdo.
        // Mantiene la alternancia ORIG/FLIP cuando mirrorTileB=true (cada tile conserva
        // su flip; al teletransportarse a la derecha del otro la alternancia se preserva).
        _tileA.localPosition += Vector3.left * delta;
        _tileB.localPosition += Vector3.left * delta;

        float threshold = -tileWidth;
        float step      = tileWidth - tileOverlap;

        if (_tileA.localPosition.x <= threshold)
        {
            Vector3 p = _tileB.localPosition;
            p.x += step;
            _tileA.localPosition = p;
        }
        else if (_tileB.localPosition.x <= threshold)
        {
            Vector3 p = _tileA.localPosition;
            p.x += step;
            _tileB.localPosition = p;
        }
    }

    void ScrollSingle(float delta)
    {
        transform.Translate(Vector3.left * delta, Space.World);

        // Usamos el borde DERECHO del sprite (pivot + semiancho) como referencia
        // para asegurarnos de que el sprite esté completamente fuera de pantalla
        // antes de reposicionarlo.
        float rightEdge = transform.position.x + _singleHalfWidth;
        if (rightEdge < -respawnX)
        {
            Vector3 p = transform.position;
            p.x = respawnX + _singleHalfWidth;
            transform.position = p;
        }
    }

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
