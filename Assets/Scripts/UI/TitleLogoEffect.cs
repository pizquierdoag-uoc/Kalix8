using System.Collections;
using UnityEngine;

public class TitleLogoEffect : MonoBehaviour
{
    [Header("Sprites de letra")]
    public Sprite spriteK;
    public Sprite spriteA;
    public Sprite spriteL;
    public Sprite spriteI;
    public Sprite spriteX;
    public Sprite sprite8;

    [Header("Posición final del grupo")]
    public Vector3 targetPosition = new Vector3(0f, 2.2f, 0f);

    [Header("Layout")]
    public float letterSpacing = 1.55f;
    public float letterScale   = 1.40f;
    public float eightExtraGap = 3.0f;

    [Header("Entrada")]
    public float entryDuration = 0.50f;
    public float letterStagger = 0.08f;
    public float entryZoomFrom = 4.00f;
    public float entryRotDeg   = 18f;
    public float entryOffsetY  = 4.50f;

    [Header("Flotación idle")]
    public float hoverAmplitude   = 0.09f;
    public float hoverFrequency   = 0.60f;
    public float hoverPhaseOffset = 1.8f;

    public bool EntryComplete { get; private set; }

    const string LETTERS = "KALIX8";

    GameObject[]     _letterGO;
    SpriteRenderer[] _letterSR;
    float[]          _letterScales;
    bool             _idleActive;
    float            _idleTime;
    Vector3          _basePosition;

    void Awake()
    {
        var oldSR = GetComponent<SpriteRenderer>();
        if (oldSR != null) oldSR.enabled = false;
    }

    void Start()
    {
        BuildLetters();
        SetAllVisible(false);
    }

    void Update()
    {
        if (!_idleActive) return;
        _idleTime += Time.deltaTime;
        float yOff = Mathf.Sin((_idleTime + hoverPhaseOffset) * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = _basePosition + new Vector3(0f, yOff, 0f);
    }

    public void Hide()
    {
        EntryComplete = false;
        _idleActive   = false;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.position = targetPosition + Vector3.up * entryOffsetY;
        EntryComplete      = false;
        _idleActive        = false;
        SetAllVisible(false);
    }

    public void PlayEntry()
    {
        StartCoroutine(EntryRoutine());
    }

    IEnumerator EntryRoutine()
    {
        Vector3 startPos = transform.position;
        float   fallDur  = 0.35f;
        for (float t = 0; t < 1f; t += Time.deltaTime / fallDur)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        transform.position = targetPosition;

        for (int i = 0; i < 5; i++)
            StartCoroutine(LetterZoomIn(i, i * letterStagger));

        float totalWait = entryDuration + 4f * letterStagger + 0.06f;
        yield return new WaitForSeconds(totalWait);

        yield return StartCoroutine(Letter8SpecialRoutine());

        _basePosition = targetPosition;
        EntryComplete = true;
        _idleActive   = true;
        _idleTime     = 0f;
    }

    IEnumerator LetterZoomIn(int index, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        var go = _letterGO[index];
        var sr = _letterSR[index];
        go.SetActive(true);

        float rotDir  = (index % 2 == 0) ? 1f : -1f;
        float elapsed = 0f;

        while (elapsed < entryDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / entryDuration);
            float te    = EaseOutBack(t);
            float finalS = _letterScales[index];

            go.transform.localScale    = new Vector3(Mathf.Lerp(entryZoomFrom * finalS, finalS, te), Mathf.Lerp(entryZoomFrom * finalS, finalS, te), 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(entryRotDeg * rotDir, 0f, te));
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 4f));

            yield return null;
        }

        go.transform.localScale    = Vector3.one * _letterScales[index];
        go.transform.localRotation = Quaternion.identity;
        sr.color = Color.white;
    }

    IEnumerator Letter8SpecialRoutine()
    {
        int   idx    = 5;
        var   go     = _letterGO[idx];
        var   sr     = _letterSR[idx];
        float finalS = _letterScales[idx];

        go.SetActive(true);

        // Fase 1: Entrada con zoom (0.9s)
        float elapsed = 0f, entryLen = 0.9f;
        while (elapsed < entryLen)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / entryLen);
            float te = EaseOutBack(t);
            go.transform.localScale    = Vector3.one * Mathf.Lerp(entryZoomFrom * finalS, finalS, te);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(entryRotDeg, 0f, te));
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t * 4f));
            yield return null;
        }
        go.transform.localScale    = Vector3.one * finalS;
        go.transform.localRotation = Quaternion.identity;
        sr.color = Color.white;

        // Fase 2: Giro completo 360° (2.6s)
        elapsed = 0f;
        float spinLen = 2.6f;
        while (elapsed < spinLen)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Lerp(0f, 360f, EaseInOutCubic(Mathf.Clamp01(elapsed / spinLen)));
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        go.transform.localRotation = Quaternion.identity;

        // Fase 3: Shake de posición decreciente (1.5s)
        elapsed = 0f;
        float shakeLen    = 1.5f;
        Vector3 baseLocal = go.transform.localPosition;
        while (elapsed < shakeLen)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - elapsed / shakeLen;
            float xOff  = Mathf.Sin(elapsed * 55f) * 0.4f * finalS * decay;
            float yOff  = Mathf.Sin(elapsed * 80f) * 0.15f * finalS * decay;
            go.transform.localPosition = baseLocal + new Vector3(xOff, yOff, 0f);
            yield return null;
        }
        go.transform.localPosition = baseLocal;
    }

    void BuildLetters()
    {
        Sprite[] sprites = { spriteK, spriteA, spriteL, spriteI, spriteX, sprite8 };

        _letterGO     = new GameObject[6];
        _letterSR     = new SpriteRenderer[6];
        _letterScales = new float[6];

        float[] glyphHeights = new float[6];
        float   maxH         = 0f;
        for (int i = 0; i < 6; i++)
        {
            glyphHeights[i] = GlyphHeight(sprites[i]);
            if (glyphHeights[i] > maxH) maxH = glyphHeights[i];
        }
        if (maxH <= 0f) maxH = 1f;

        float totalWidth = 5f * letterSpacing + eightExtraGap;
        float startX     = -totalWidth * 0.5f;

        for (int i = 0; i < 6; i++)
        {
            float s    = letterScale * (maxH / Mathf.Max(glyphHeights[i], 0.001f));
            float xPos = startX + i * letterSpacing + (i == 5 ? eightExtraGap : 0f);

            _letterScales[i] = s;

            var go = new GameObject("Letter_" + LETTERS[i]);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(xPos, 0f, 0f);
            go.transform.localScale    = Vector3.one * s;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprites[i];
            sr.color        = Color.clear;
            sr.sortingOrder = 10;

            _letterGO[i] = go;
            _letterSR[i] = sr;
        }
    }

    static float GlyphHeight(Sprite sp)
    {
        if (sp == null) return 1f;
        var tex = sp.texture;
        if (!tex.isReadable) return sp.bounds.size.y;

        var pixels = tex.GetPixels32();
        int w = tex.width, h = tex.height;
        int minY = h, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (pixels[y * w + x].a > 10)
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
        if (maxY < 0) return sp.bounds.size.y;
        return (maxY - minY + 1) / sp.pixelsPerUnit;
    }

    void SetAllVisible(bool visible)
    {
        if (_letterGO == null) return;
        foreach (var go in _letterGO)
            if (go != null) go.SetActive(visible);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    static float EaseInOutCubic(float t)
        => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
}
