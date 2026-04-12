using UnityEngine;

public class FrameAnimator : MonoBehaviour
{
    [Header("Frames de la animación")]
    public Sprite[] frames;

    [Header("Velocidad (frames por segundo)")]
    public float fps = 12f;

    [Header("¿Loop?")]
    public bool loop = true;

    SpriteRenderer _sr;
    float          _timer;
    int            _current;
    bool           _playing = true;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0 && _sr != null)
            _sr.sprite = frames[0];
    }

    void Update()
    {
        if (!_playing || frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < 1f / fps) return;

        _timer = 0f;
        _current++;

        if (_current >= frames.Length)
        {
            if (loop)
                _current = 0;
            else
            {
                _current = frames.Length - 1;
                _playing = false;
                return;
            }
        }

        if (_sr != null) _sr.sprite = frames[_current];
    }

    public void Play()  { _current = 0; _timer = 0f; _playing = true; }
    public void Stop()  { _playing = false; }
    public bool IsDone  => !_playing && !loop;
}
