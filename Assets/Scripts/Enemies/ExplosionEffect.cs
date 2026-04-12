using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Frames de la explosión (de Explosion_0 a Explosion_7)")]
    public Sprite[] frames;

    [Header("Frames por segundo")]
    public float fps = 18f;

    SpriteRenderer _sr;
    float          _timer;
    int            _current;

    void OnEnable()
    {
        _sr      = GetComponent<SpriteRenderer>();
        _current = 0;
        _timer   = 0f;
        if (_sr != null && frames != null && frames.Length > 0)
            _sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < 1f / fps) return;
        _timer = 0f;

        _current++;
        if (_current >= frames.Length)
        {
            gameObject.SetActive(false);
            return;
        }

        if (_sr != null) _sr.sprite = frames[_current];
    }

    public static void Spawn(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, position, Quaternion.identity);
        go.SetActive(true);
    }
}
