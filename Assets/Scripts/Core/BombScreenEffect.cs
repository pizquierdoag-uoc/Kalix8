using System.Collections;
using UnityEngine;

public class BombScreenEffect : MonoBehaviour
{
    public static BombScreenEffect Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Frames de la explosión de bomba")]
    public Sprite[] frames;

    [Header("Configuración")]
    public int   totalExplosions   = 20;
    public float totalDuration     = 4f;
    public float explosionDuration = 1f;
    public float explosionScale    = 4f;
    public int   sortingOrder      = 20;
    public string sortingLayerName = "Foreground";
    public Material spriteMaterial; // Asigna "Sprite-Unlit-Default" en Inspector

    public void Trigger()
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("[BombScreenEffect] frames vacío.");
            return;
        }
        bool anyValid = System.Array.Exists(frames, f => f != null);
        if (!anyValid) { Debug.LogError("[BombScreenEffect] Todos los sprites son null."); return; }
        StartCoroutine(SpawnSequence());
    }

    IEnumerator SpawnSequence()
    {
        float interval = totalDuration / totalExplosions;
        Camera cam = Camera.main;

        for (int i = 0; i < totalExplosions; i++)
        {
            SpawnOne(cam);
            // Realtime para que no quede colgada si el juego se pausa durante la animación
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    void SpawnOne(Camera cam)
    {
        if (cam == null) return;

        float depth = Mathf.Abs(cam.transform.position.z);
        if (depth < 0.1f) depth = 10f;
        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0.1f, 0.1f, depth));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(0.9f, 0.9f, depth));

        Vector3 pos = new Vector3(
            Random.Range(bl.x, tr.x),
            Random.Range(bl.y, tr.y),
            0f
        );

        var go = new GameObject("BombFX");
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * explosionScale;

        // Asignar frames ANTES de activar el componente para que OnEnable los vea
        go.SetActive(false);

        var sr = go.AddComponent<SpriteRenderer>();
        // Usa siempre el sorting layer más alto del proyecto (el más al frente)
        var allLayers = SortingLayer.layers;
        sr.sortingLayerID = allLayers.Length > 0 ? allLayers[allLayers.Length - 1].id : 0;
        sr.sortingOrder   = sortingOrder;
        if (spriteMaterial != null)
            sr.material = spriteMaterial;
        else
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                      ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
                sr.material = new Material(shader);
            else
                Debug.LogWarning("[BombScreenEffect] spriteMaterial no asignado en Inspector. " +
                                 "La explosión de bomba puede no verse en build. Asigna un material URP en el componente.");
        }

        var fx = go.AddComponent<ExplosionEffect>();
        fx.frames   = frames;
        fx.duration = explosionDuration;

        go.SetActive(true);
        Destroy(go, explosionDuration + 0.1f);
    }
}
