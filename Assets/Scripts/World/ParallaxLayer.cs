using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Velocidad relativa (0=quieto, 1=velocidad base)")]
    [Range(0f, 2f)] public float speedMultiplier = 0.5f;

    [Header("Ancho de un tile en unidades Unity (pixels / PPU)")]
    public float tileWidth = 20f;

    [Header("Modo objeto único (para el planeta)")]
    public bool  singleObject = false;
    public float respawnX     = 30f;

    [HideInInspector] public float baseScrollSpeed;

    Transform _tileA;
    Transform _tileB;

    void Awake()
    {
        if (singleObject) return;
        if (transform.childCount < 2)
        {
            Debug.LogError($"[ParallaxLayer] '{gameObject.name}' necesita 2 hijos (TileA y TileB).");
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
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        float move = baseScrollSpeed * speedMultiplier * Time.deltaTime;
        if (singleObject) UpdateSingleObject(move);
        else              UpdateTiledLayer(move);
    }

    void UpdateTiledLayer(float move)
    {
        transform.Translate(Vector3.left * move);
        if (_tileA.position.x < -tileWidth) _tileA.localPosition = _tileB.localPosition + Vector3.right * tileWidth;
        if (_tileB.position.x < -tileWidth) _tileB.localPosition = _tileA.localPosition + Vector3.right * tileWidth;
    }

    void UpdateSingleObject(float move)
    {
        transform.Translate(Vector3.left * move);
        if (transform.position.x < -respawnX)
        {
            Vector3 pos = transform.position;
            pos.x = respawnX;
            transform.position = pos;
        }
    }

    public void Pause()  => enabled = false;
    public void Resume() => enabled = true;
}
