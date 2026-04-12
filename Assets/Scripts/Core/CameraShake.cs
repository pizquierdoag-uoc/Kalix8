using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Range(0f, 2f)] public float maxIntensity = 0.5f;

    Vector3 _originalPos;
    bool _shaking;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnEnable()  => _originalPos = transform.localPosition;
    void OnDisable() => transform.localPosition = _originalPos;

    /// <summary>Sacude la cámara. intensity se clampea a maxIntensity.</summary>
    public void Shake(float intensity, float duration)
    {
        if (!isActiveAndEnabled) return;
        intensity = Mathf.Min(intensity, maxIntensity);
        if (_shaking) StopAllCoroutines();
        StartCoroutine(ShakeRoutine(intensity, duration));
    }

    IEnumerator ShakeRoutine(float intensity, float duration)
    {
        _shaking = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration);
            float current = intensity * t;
            transform.localPosition = _originalPos + (Vector3)Random.insideUnitCircle * current;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originalPos;
        _shaking = false;
    }
}
