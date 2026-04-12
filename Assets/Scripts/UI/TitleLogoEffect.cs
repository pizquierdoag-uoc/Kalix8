using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TitleLogoEffect : MonoBehaviour
{
    [Header("Posición final")]
    public Vector3 targetPosition = new Vector3(0f, 2.2f, 0f);

    [Header("Animación de entrada")]
    public float entryDuration = 0.85f;
    [Tooltip("Desplazamiento de inicio (sobre el target). La logo cae desde arriba.")]
    public float entryOffsetY = 4.5f;

    [Header("Flotación idle")]
    public float hoverAmplitude   = 0.10f;
    public float hoverFrequency   = 0.65f;
    [Tooltip("Desfase de fase respecto a la nave para movimiento orgánico.")]
    public float hoverPhaseOffset = 1.8f;

    [Header("Efecto arco iris")]
    [Range(0.05f, 2f)]
    public float rainbowSpeed      = 0.25f;  // Ciclos de color por segundo
    [Range(0f, 1f)]
    public float rainbowSaturation = 0.70f;
    [Range(0.5f, 1f)]
    public float rainbowBrightness = 1.00f;

    [Header("Shimmer (destello deslizante)")]
    public bool  useShimmer    = true;
    public float shimmerSpeed  = 2.2f;   // Unidades/segundo
    public float shimmerWidth  = 1.0f;   // Ancho de la franja en unidades Unity
    [Range(0f, 1f)]
    public float shimmerAlpha  = 0.55f;

    public bool EntryComplete { get; private set; }

    bool  _active;
    float _time;
    float _shimmerX;
    float _logoHalfWidth;

    SpriteRenderer _sr;
    SpriteRenderer _shimmerSr;
    GameObject     _shimmerGo;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (useShimmer) BuildShimmer();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        EntryComplete = false;
        _active       = false;
        if (_shimmerGo != null) _shimmerGo.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.position   = targetPosition + Vector3.up * entryOffsetY;
        transform.localScale = Vector3.zero;
        EntryComplete        = false;
        _active              = false;
        _sr.color            = Color.white;
        if (_shimmerGo != null) _shimmerGo.SetActive(false);
    }

    public void PlayEntry()
    {
        // Calcular el semi-ancho del logo para el shimmer
        if (_sr.sprite != null)
            _logoHalfWidth = _sr.sprite.bounds.extents.x * transform.lossyScale.x;
        else
            _logoHalfWidth = 2.5f;  // Valor de fallback

        StartCoroutine(EntryRoutine());
    }

    IEnumerator EntryRoutine()
    {
        Vector3 startPos = transform.position;
        float elapsed    = 0f;

        while (elapsed < entryDuration)
        {
            elapsed     += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / entryDuration);
            float scale  = EaseOutBack(t);
            transform.localScale = Vector3.one * Mathf.Max(0f, scale);
            transform.position   = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.localScale = Vector3.one;
        transform.position   = targetPosition;

        EntryComplete = true;
        _active       = true;
        _time         = 0f;

        // El shimmer empieza fuera del logo por la izquierda
        _shimmerX = -_logoHalfWidth - shimmerWidth;
        if (_shimmerGo != null) _shimmerGo.SetActive(true);
    }

    void Update()
    {
        if (!_active) return;
        _time += Time.deltaTime;

        // Arco iris: cicla el matiz HSV sobre el SpriteRenderer
        float hue = Mathf.Repeat(_time * rainbowSpeed, 1f);
        _sr.color = Color.HSVToRGB(hue, rainbowSaturation, rainbowBrightness);

        // Flotación suave (desfasada respecto a la nave)
        float yOff = Mathf.Sin((_time + hoverPhaseOffset) * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = targetPosition + new Vector3(0f, yOff, 0f);

        // Shimmer: la franja recorre el logo de izquierda a derecha
        if (_shimmerGo != null && _shimmerGo.activeSelf)
        {
            _shimmerX += Time.deltaTime * shimmerSpeed;

            // Cuando sale por la derecha, vuelve a la izquierda con una pausa
            float rightEdge = _logoHalfWidth + shimmerWidth;
            if (_shimmerX > rightEdge)
                _shimmerX = -_logoHalfWidth - shimmerWidth * 3f; // Pausa extra

            Vector3 sp = _shimmerGo.transform.localPosition;
            sp.x = _shimmerX;
            _shimmerGo.transform.localPosition = sp;
        }
    }

    // Construye el objeto de shimmer (franja blanca degradada)
    void BuildShimmer()
    {
        _shimmerGo = new GameObject("Shimmer");
        _shimmerGo.transform.SetParent(transform, false);
        _shimmerGo.transform.localPosition = Vector3.zero;

        _shimmerSr                  = _shimmerGo.AddComponent<SpriteRenderer>();
        _shimmerSr.sortingLayerName = _sr.sortingLayerName;
        _shimmerSr.sortingOrder     = _sr.sortingOrder + 1;
        _shimmerSr.sprite           = CreateShimmerSprite();
        _shimmerSr.color            = new Color(1f, 1f, 1f, shimmerAlpha);

        _shimmerGo.SetActive(false);
    }

    // Genera en tiempo de ejecución un degradado horizontal blanco→transparente
    Sprite CreateShimmerSprite()
    {
        const int w = 64, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var pixels = new Color[w * h];
        for (int x = 0; x < w; x++)
        {
            float t     = (float)x / (w - 1);
            float alpha = Mathf.Sin(t * Mathf.PI); // 0→1→0 a lo ancho
            for (int y = 0; y < h; y++)
                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
        }
        tex.SetPixels(pixels);
        tex.Apply();

        // PPU ajustado para que el sprite ocupe ~shimmerWidth unidades al escalar 1:1
        float ppu = w / shimmerWidth;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }

    // EaseOutBack: llega al target y se pasa ligeramente antes de asentarse
    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
