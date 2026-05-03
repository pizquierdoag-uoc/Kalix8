using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Frames de la explosión")]
    public Sprite[] frames;

    [Header("Duración total en segundos")]
    public float duration = 1f;

    SpriteRenderer _sr;
    float          _elapsed;

    void OnEnable()
    {
        _sr      = GetComponent<SpriteRenderer>();
        _elapsed = 0f;
        if (_sr != null && frames != null && frames.Length > 0)
            _sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _elapsed += Time.deltaTime;

        float t = _elapsed / duration;
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        int idx = Mathf.FloorToInt(t * frames.Length);
        if (_sr != null) _sr.sprite = frames[idx];
    }

    public static void Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, position, Quaternion.identity);
        go.SetActive(true);
    }
}
