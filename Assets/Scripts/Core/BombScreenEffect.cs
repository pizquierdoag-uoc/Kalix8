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
        Debug.Log($"[BombScreenEffect] Trigger llamado. GameObject={gameObject.name} Scene={gameObject.scene.name}");
        if (frames == null || frames.Length == 0)
        {
            Debug.LogError("[BombScreenEffect] frames vacío.");
            return;
        }
        int nonNull = 0;
        foreach (var f in frames) if (f != null) nonNull++;
        Debug.Log($"[BombScreenEffect] {frames.Length} slots, {nonNull} válidos, layer={sortingLayerName}, material={spriteMaterial}");
        if (nonNull == 0) { Debug.LogError("[BombScreenEffect] Todos los sprites son null."); return; }
        StartCoroutine(SpawnSequence());
    }

    IEnumerator SpawnSequence()
    {
        float interval = totalDuration / totalExplosions;
        Camera cam = Camera.main;

        for (int i = 0; i < totalExplosions; i++)
        {
            SpawnOne(cam);
            yield return new WaitForSeconds(interval);
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
                      ?? Shader.Find("Sprites/Default");
            if (shader != null) sr.material = new Material(shader);
        }

        var fx = go.AddComponent<ExplosionEffect>();
        fx.frames   = frames;
        fx.duration = explosionDuration;

        go.SetActive(true);
        Debug.Log($"[BombFX] pos={pos} layer='{sr.sortingLayerName}' order={sr.sortingOrder} mat='{sr.material?.name}' sprite='{sr.sprite?.name}'");

        Destroy(go, explosionDuration + 0.1f);
    }
}
