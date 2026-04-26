using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class SetupPlayerAndHUD
{
    [MenuItem("Kalix8/Setup — Sprite nave + Contador bombas")]
    static void Run()
    {
        int changes = 0;
        changes += ChangePlayerSprite();
        changes += CreateBombCounter();
        if (changes > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[Setup] Listo — {changes} cambio(s) aplicado(s). Guarda la escena con Ctrl+S.");
        }
        else
        {
            Debug.Log("[Setup] Nada que cambiar.");
        }
    }

    // ── 1. Cambia el sprite del jugador ──────────────────────────────────
    static int ChangePlayerSprite()
    {
        const string GUID = "d4ded1ba73f646644ab1aa12c47af75b";
        string path = AssetDatabase.GUIDToAssetPath(GUID);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("[Setup] nave_sin_fondo.png no encontrado — importa el sprite primero.");
            return 0;
        }

        Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (newSprite == null)
        {
            Debug.LogWarning("[Setup] El asset no tiene tipo Sprite. Cambia Texture Type a 'Sprite (2D and UI)' en el Inspector y reimporta.");
            return 0;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogWarning("[Setup] No se encontró el GameObject con tag 'Player'."); return 0; }

        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr == null) { Debug.LogWarning("[Setup] El Player no tiene SpriteRenderer."); return 0; }

        Undo.RecordObject(sr, "Cambiar sprite jugador");
        sr.sprite = newSprite;
        Debug.Log($"[Setup] Sprite del jugador cambiado a: {path}");
        return 1;
    }

    // ── 2. Crea el contador de bombas en el HUD ──────────────────────────
    static int CreateBombCounter()
    {
        HUDController hud = Object.FindAnyObjectByType<HUDController>();
        if (hud == null) { Debug.LogWarning("[Setup] HUDController no encontrado en la escena."); return 0; }

        if (hud.bombIcon != null)
        {
            Debug.Log("[Setup] El contador de bombas ya existe — no se modifica.");
            return 0;
        }

        // Busca el grupo de iconos de vida para posicionarse debajo
        Transform lifeGroup = null;
        if (hud.lifeIcons != null && hud.lifeIcons.Length > 0 && hud.lifeIcons[0] != null)
            lifeGroup = hud.lifeIcons[0].transform.parent;

        Transform hudParent = lifeGroup != null ? lifeGroup.parent : hud.transform;

        // ── Contenedor ──
        GameObject container = new GameObject("BombCounter");
        Undo.RegisterCreatedObjectUndo(container, "Crear BombCounter");
        container.transform.SetParent(hudParent, false);

        RectTransform cRT = container.AddComponent<RectTransform>();

        // Posiciona debajo del grupo de vidas
        if (lifeGroup != null)
        {
            RectTransform lgRT = lifeGroup.GetComponent<RectTransform>();
            if (lgRT != null)
            {
                cRT.anchorMin        = lgRT.anchorMin;
                cRT.anchorMax        = lgRT.anchorMax;
                cRT.anchoredPosition = lgRT.anchoredPosition + new Vector2(0, -50f);
                cRT.sizeDelta        = new Vector2(120, 36);
                cRT.pivot            = lgRT.pivot;
            }
        }
        else
        {
            cRT.anchorMin        = new Vector2(0, 1);
            cRT.anchorMax        = new Vector2(0, 1);
            cRT.anchoredPosition = new Vector2(20, -160);
            cRT.sizeDelta        = new Vector2(120, 36);
            cRT.pivot            = new Vector2(0, 1);
        }

        // ── Icono (Image con sprite de bomba o "B" como texto) ──
        GameObject iconGO = new GameObject("BombIcon");
        Undo.RegisterCreatedObjectUndo(iconGO, "Crear BombIcon");
        iconGO.transform.SetParent(container.transform, false);

        Image img = iconGO.AddComponent<Image>();
        img.color = new Color(1f, 0.35f, 0.15f);  // naranja bomba

        // Intenta cargar el sprite PU_Bomb como icono
        Sprite bombSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Sprites/PowerUps/PU_Bomb.png");
        if (bombSprite != null) img.sprite = bombSprite;

        RectTransform iRT = iconGO.GetComponent<RectTransform>();
        iRT.anchorMin = iRT.anchorMax = new Vector2(0, 0.5f);
        iRT.pivot     = new Vector2(0, 0.5f);
        iRT.sizeDelta = new Vector2(28, 28);
        iRT.anchoredPosition = Vector2.zero;

        // ── Texto contador ──
        GameObject txtGO = new GameObject("TxtBombCount");
        Undo.RegisterCreatedObjectUndo(txtGO, "Crear TxtBombCount");
        txtGO.transform.SetParent(container.transform, false);

        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "×2";
        tmp.fontSize  = 22;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform tRT = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = tRT.anchorMax = new Vector2(0, 0.5f);
        tRT.pivot     = new Vector2(0, 0.5f);
        tRT.sizeDelta = new Vector2(70, 36);
        tRT.anchoredPosition = new Vector2(34, 0);

        // ── Conecta al HUDController ──
        Undo.RecordObject(hud, "Conectar BombCounter al HUD");
        hud.bombIcon     = img;
        hud.txtBombCount = tmp;

        Debug.Log("[Setup] Contador de bombas creado y conectado al HUDController.");
        return 1;
    }
}
