using UnityEngine;

public class ScrollManager : MonoBehaviour
{
    [Header("Velocidad base (unidades/segundo)")]
    public float scrollSpeed = 5f;

    [Header("Capas — arrastra cada ParallaxLayer aquí")]
    public ParallaxLayer[] layers;

    [Header("Rampa de aceleración al inicio (segundos)")]
    public float rampUpDuration = 1.5f;

    float _currentSpeed;
    float _targetSpeed;
    float _rampTimer;
    bool  _ramping;

    void Awake()
    {
        if (layers == null || layers.Length == 0)
            layers = GetComponentsInChildren<ParallaxLayer>();

        _targetSpeed  = scrollSpeed;
        _currentSpeed = 0f;
        _ramping      = true;
        _rampTimer    = 0f;
        ApplySpeed(0f);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (!_ramping) return;
        _rampTimer   += Time.deltaTime;
        float t       = Mathf.Clamp01(_rampTimer / rampUpDuration);
        _currentSpeed = Mathf.Lerp(0f, _targetSpeed, t);
        ApplySpeed(_currentSpeed);
        if (t >= 1f) _ramping = false;
    }

    public void SetSpeed(float newSpeed)
    {
        scrollSpeed = _targetSpeed = _currentSpeed = newSpeed;
        _ramping    = false;
        ApplySpeed(newSpeed);
    }

    public void RampToSpeed(float newSpeed, float duration)
    {
        scrollSpeed    = _targetSpeed = newSpeed;
        rampUpDuration = duration;
        _rampTimer     = 0f;
        _ramping       = true;
    }

    public void PauseScroll()  { foreach (var l in layers) l?.Pause(); }
    public void ResumeScroll() { foreach (var l in layers) l?.Resume(); }

    void ApplySpeed(float speed) { foreach (var l in layers) { if (l != null) l.baseScrollSpeed = speed; } }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f,
            $"Scroll: {_currentSpeed:F1} u/s");
    }
#endif
}
