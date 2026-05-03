using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Logo "KALIX8" con efecto arcade After Burner: zoom-in escalonado por letra,
/// rotación de entrada, colores neón individuales y pulso idle.
/// Genera los sprites de píxel-art en tiempo de ejecución — no requiere assets externos.
/// </summary>
public class TitleLogoEffect : MonoBehaviour
{
    // ── Sprites opcionales (si se asignan en el Inspector sustituyen a los generados) ──
    [Header("Sprites de letra (opcional — se generan automáticamente si están vacíos)")]
    public Sprite spriteK;
    public Sprite spriteA;
    public Sprite spriteL;
    public Sprite spriteI;
    public Sprite spriteX;
    public Sprite sprite8;

    // ── Layout ──────────────────────────────────────────────────────────────────────
    [Header("Posición final del grupo")]
    public Vector3 targetPosition = new Vector3(0f, 2.2f, 0f);

    [Header("Layout")]
    public float letterSpacing = 1.55f;
    public float letterScale   = 1.40f;

    // ── Animación de entrada ─────────────────────────────────────────────────────────
    [Header("Entrada (After Burner)")]
    public float entryDuration = 0.50f;
    public float letterStagger = 0.08f;
    public float entryZoomFrom = 4.00f;
    public float entryRotDeg   = 18f;
    public float entryOffsetY  = 4.50f;

    // ── Idle ────────────────────────────────────────────────────────────────────────
    [Header("Pulso idle")]
    public float pulseSpeed = 1.10f;
    [Range(0.55f, 1f)]
    public float pulseMin   = 0.60f;

    [Header("Flotación idle")]
    public float hoverAmplitude   = 0.09f;
    public float hoverFrequency   = 0.60f;
    public float hoverPhaseOffset = 1.8f;

    // ── API ─────────────────────────────────────────────────────────────────────────
    public bool EntryComplete { get; private set; }

    // ── Privados ─────────────────────────────────────────────────────────────────────
    const string LETTERS = "KALIX8";

    GameObject[]     _letterGO;
    SpriteRenderer[] _letterSR;
    bool             _idleActive;
    float            _idleTime;
    Vector3          _basePosition;

    // Colores neón arcade por letra
    static readonly Color[] LetterColors =
    {
        new Color(1.00f, 0.85f, 0.00f),  // K — amarillo
        new Color(1.00f, 0.50f, 0.00f),  // A — naranja
        new Color(1.00f, 0.85f, 0.00f),  // L — amarillo
        new Color(0.30f, 0.90f, 1.00f),  // I — cian
        new Color(1.00f, 0.50f, 0.00f),  // X — naranja
        new Color(1.00f, 0.15f, 0.15f),  // 8 — rojo
    };

    // Fuente bitmap 5 × 7 píxeles. Cada int es una fila; bit 4 = columna izquierda.
    static readonly Dictionary<char, int[]> Font5x7 = new Dictionary<char, int[]>
    {
        { 'K', new[] { 0b10001, 0b10010, 0b10100, 0b11000, 0b10100, 0b10010, 0b10001 } },
        { 'A', new[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 } },
        { 'L', new[] { 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b10000, 0b11111 } },
        { 'I', new[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b11111 } },
        { 'X', new[] { 0b10001, 0b01010, 0b00100, 0b00100, 0b01010, 0b10001, 0b00000 } },
        { '8', new[] { 0b01110, 0b10001, 0b10001, 0b01110, 0b10001, 0b10001, 0b01110 } },
    };

    // ── Unity lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        // Desactiva el SpriteRenderer legacy (sprite Kalix8.png del GO padre)
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

        // Flotación global
        float yOff = Mathf.Sin((_idleTime + hoverPhaseOffset) * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = _basePosition + new Vector3(0f, yOff, 0f);

        // Pulso de brillo individual por letra
        for (int i = 0; i < _letterSR.Length; i++)
        {
            if (_letterSR[i] == null) continue;
            float phase  = _idleTime * pulseSpeed + i * 0.42f;
            float bright = Mathf.Lerp(pulseMin, 1f, (Mathf.Sin(phase * Mathf.PI * 2f) + 1f) * 0.5f);
            Color c      = LetterColors[i];
            _letterSR[i].color = new Color(c.r * bright, c.g * bright, c.b * bright, 1f);
        }
    }

    // ── API pública ──────────────────────────────────────────────────────────────────

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

    // ── Coroutinas de animación ──────────────────────────────────────────────────────

    IEnumerator EntryRoutine()
    {
        // Fase 1: el grupo cae desde arriba hasta targetPosition
        Vector3 startPos = transform.position;
        float fallDur    = 0.35f;
        for (float t = 0; t < 1f; t += Time.deltaTime / fallDur)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, EaseOutCubic(Mathf.Clamp01(t)));
            yield return null;
        }
        transform.position = targetPosition;

        // Fase 2: cada letra hace zoom-in con stagger
        for (int i = 0; i < 6; i++)
            StartCoroutine(LetterZoomIn(i, i * letterStagger));

        // Espera a que termine la última letra
        float totalWait = entryDuration + 5f * letterStagger + 0.06f;
        yield return new WaitForSeconds(totalWait);

        // Fase 3: impacto — pequeña sacudida de escala del grupo
        yield return StartCoroutine(GroupImpactShake());

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

        Color baseColor = LetterColors[index];
        float rotDir    = (index % 2 == 0) ? 1f : -1f;
        float elapsed   = 0f;

        while (elapsed < entryDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / entryDuration);
            float te = EaseOutBack(t);

            float s = Mathf.Lerp(entryZoomFrom * letterScale, letterScale, te);
            go.transform.localScale = new Vector3(s, s, 1f);

            float alpha = Mathf.Clamp01(t * 4f);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            float rot = Mathf.Lerp(entryRotDeg * rotDir, 0f, te);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, rot);

            yield return null;
        }

        go.transform.localScale    = Vector3.one * letterScale;
        go.transform.localRotation = Quaternion.identity;
        sr.color = baseColor;
    }

    IEnumerator GroupImpactShake()
    {
        float[] keyScales = { 1.10f, 0.93f, 1.04f, 0.98f, 1.00f };
        float   dur       = 0.04f;

        foreach (float target in keyScales)
        {
            float start = transform.localScale.x;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float s = Mathf.Lerp(start, target, t / dur);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }
        transform.localScale = Vector3.one;
    }

    // ── Construcción de letras ───────────────────────────────────────────────────────

    void BuildLetters()
    {
        Sprite[] sprites =
        {
            spriteK != null ? spriteK : GenerateLetterSprite('K'),
            spriteA != null ? spriteA : GenerateLetterSprite('A'),
            spriteL != null ? spriteL : GenerateLetterSprite('L'),
            spriteI != null ? spriteI : GenerateLetterSprite('I'),
            spriteX != null ? spriteX : GenerateLetterSprite('X'),
            sprite8 != null ? sprite8 : GenerateLetterSprite('8'),
        };

        _letterGO = new GameObject[6];
        _letterSR = new SpriteRenderer[6];

        float totalWidth = 5f * letterSpacing;
        float startX     = -totalWidth * 0.5f;

        for (int i = 0; i < 6; i++)
        {
            var go = new GameObject("Letter_" + LETTERS[i]);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(startX + i * letterSpacing, 0f, 0f);
            go.transform.localScale    = Vector3.one * letterScale;

            var sr          = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sprites[i];
            sr.color        = Color.clear;
            sr.sortingOrder = 10;

            _letterGO[i] = go;
            _letterSR[i] = sr;
        }
    }

    void SetAllVisible(bool visible)
    {
        if (_letterGO == null) return;
        foreach (var go in _letterGO)
            if (go != null) go.SetActive(visible);
    }

    // ── Generación de sprite de píxel-art ───────────────────────────────────────────

    Sprite GenerateLetterSprite(char ch)
    {
        if (!Font5x7.TryGetValue(ch, out int[] rows))
            rows = Font5x7['K'];

        const int scale    = 7;
        const int borderPx = 2;
        const int pad      = 5;

        int texW = 5 * scale + pad * 2;
        int texH = 7 * scale + pad * 2;

        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp,
            name       = "Letter_" + ch
        };

        bool[,] mask = new bool[texW, texH];
        for (int row = 0; row < 7; row++)
        {
            int bits = (row < rows.Length) ? rows[row] : 0;
            for (int col = 0; col < 5; col++)
            {
                bool on = (bits & (1 << (4 - col))) != 0;
                if (!on) continue;

                int px = pad + col * scale;
                int py = pad + (6 - row) * scale;
                for (int dy = 0; dy < scale; dy++)
                for (int dx = 0; dx < scale; dx++)
                    mask[px + dx, py + dy] = true;
            }
        }

        var pixels = new Color32[texW * texH];
        for (int y = 0; y < texH; y++)
        for (int x = 0; x < texW; x++)
        {
            if (mask[x, y])
            {
                pixels[y * texW + x] = new Color32(255, 255, 255, 255);
            }
            else if (IsNearMask(mask, x, y, texW, texH, borderPx))
            {
                float dist = DistToMask(mask, x, y, texW, texH, borderPx);
                byte  a    = (byte)Mathf.RoundToInt(Mathf.Lerp(180f, 0f, dist / borderPx));
                pixels[y * texW + x] = new Color32(255, 255, 255, a);
            }
            else
            {
                pixels[y * texW + x] = new Color32(0, 0, 0, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        float ppu = texW / 1.0f;
        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);
    }

    static bool IsNearMask(bool[,] mask, int x, int y, int w, int h, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h && mask[nx, ny])
                return true;
        }
        return false;
    }

    static float DistToMask(bool[,] mask, int x, int y, int w, int h, int maxR)
    {
        float best = maxR;
        for (int dy = -maxR; dy <= maxR; dy++)
        for (int dx = -maxR; dx <= maxR; dx++)
        {
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < w && ny >= 0 && ny < h && mask[nx, ny])
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < best) best = d;
            }
        }
        return best;
    }

    // ── Curvas de easing ─────────────────────────────────────────────────────────────

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
