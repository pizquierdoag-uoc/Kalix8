using UnityEngine;

public class HitSpark : MonoBehaviour
{
    static Sprite _sparkSprite;

    float _life;
    float _maxLife;
    SpriteRenderer _sr;

    // Crea el sprite de chispa proceduralmente la primera vez
    static Sprite GetOrCreateSparkSprite()
    {
        if (_sparkSprite != null) return _sparkSprite;

        const int S = 16;
        Texture2D tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[S * S];

        float cx = S * 0.5f;
        float cy = S * 0.5f;

        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dx = x - cx + 0.5f;
            float dy = y - cy + 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float r = S * 0.5f;

            // núcleo blanco brillante + halo anaranjado
            float core = Mathf.Clamp01(1f - dist / (r * 0.35f));
            float glow = Mathf.Clamp01(1f - dist / r);
            glow = glow * glow;

            float red   = Mathf.Clamp01(core + glow * 0.9f);
            float green = Mathf.Clamp01(core + glow * 0.5f);
            float blue  = Mathf.Clamp01(core);
            float alpha = Mathf.Clamp01(core * 2f + glow);

            px[y * S + x] = new Color(red, green, blue, alpha);
        }

        tex.SetPixels(px);
        tex.Apply();

        _sparkSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        return _sparkSprite;
    }

    /// <summary>Instancia un destello de impacto en <paramref name="position"/>.</summary>
    public static void Spawn(Vector3 position, Color tint, float size = 0.35f, float life = 0.12f)
    {
        var go = new GameObject("HitSpark");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateSparkSprite();
        sr.color  = tint;
        sr.transform.localScale = Vector3.one * size;
        sr.sortingLayerName = "Foreground";
        sr.sortingOrder     = 1;

        var hs = go.AddComponent<HitSpark>();
        hs._sr      = sr;
        hs._life    = life;
        hs._maxLife = life;
    }

    void Update()
    {
        _life -= Time.deltaTime;

        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // Fade-out + escala rápida
        float t = _life / _maxLife;
        _sr.color = new Color(_sr.color.r, _sr.color.g, _sr.color.b, t * t);
        transform.localScale = Vector3.one * (0.35f + (1f - t) * 0.25f);
    }
}
