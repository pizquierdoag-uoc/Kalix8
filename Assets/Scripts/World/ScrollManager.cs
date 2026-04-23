using UnityEngine;

public class ScrollManager : MonoBehaviour
{
    public static ScrollManager Instance { get; private set; }

    [Header("Velocidad base (unidades/segundo)")]
    [Min(0f)] public float baseSpeed = 5f;

    [Header("Rampa de arranque")]
    [Min(0f)] public float rampDuration = 1.5f;

    public float CurrentSpeed { get; private set; }
    public bool  IsPaused     { get; private set; }

    ParallaxLayer[] _layers;
    float _rampFrom;
    float _targetSpeed;
    float _rampDuration;   // duración interna del ramp actual (no mezclar con el campo público)
    float _rampTimer;
    bool  _ramping;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _layers = GetComponentsInChildren<ParallaxLayer>(includeInactive: true);
        BeginRamp(0f, baseSpeed, rampDuration);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (IsPaused) return;

        if (_ramping)
        {
            _rampTimer  += Time.deltaTime;
            float t      = Mathf.Clamp01(_rampTimer / _rampDuration);
            CurrentSpeed = Mathf.Lerp(_rampFrom, _targetSpeed, t * t); // ease-in
            if (t >= 1f) { CurrentSpeed = _targetSpeed; _ramping = false; }
        }

        PushSpeed(CurrentSpeed);
    }

    // Cambia la velocidad de forma instantánea.
    public void SetSpeed(float speed)
    {
        _ramping     = false;
        CurrentSpeed = speed;
        PushSpeed(speed);
    }

    // Transición suave hacia una nueva velocidad.
    public void RampTo(float speed, float duration) =>
        BeginRamp(CurrentSpeed, speed, duration);

    public void PauseScroll()
    {
        IsPaused = true;
        PushSpeed(0f);
    }

    public void ResumeScroll()
    {
        IsPaused = false;
        PushSpeed(CurrentSpeed);   // restaura velocidad inmediatamente aunque !IsPlaying
    }

    void BeginRamp(float from, float to, float duration)
    {
        _rampFrom    = from;
        _targetSpeed = to;
        _rampDuration = Mathf.Max(duration, 0.01f);  // estado interno, no toca el campo público
        _rampTimer   = 0f;
        _ramping     = true;
    }

    void PushSpeed(float speed)
    {
        foreach (var layer in _layers)
            layer?.SetBaseSpeed(speed);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up,
            $"Speed: {CurrentSpeed:F2} u/s | Paused: {IsPaused} | Ramping: {_ramping}");
    }
#endif
}
