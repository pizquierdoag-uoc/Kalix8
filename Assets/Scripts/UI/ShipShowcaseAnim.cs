using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShipShowcaseAnim : MonoBehaviour
{
    [Header("Posiciones (unidades de mundo)")]
    public Vector3 targetPosition = new Vector3(0f, -0.8f, 0f);
    public Vector3 startPosition  = new Vector3(0f, -9f,  0f);

    [Header("Animación de entrada")]
    public float          entryDuration = 2.0f;
    [Tooltip("Curva de movimiento. Por defecto se genera con overshoot.")]
    public AnimationCurve entryCurve;

    [Header("Flotación idle")]
    public float hoverAmplitude = 0.18f;
    public float hoverFrequency = 0.60f;

    [Header("Animación idle — motor")]
    [Tooltip("Frames del motor para el idle (se hace ping-pong).")]
    public Sprite[] idleFrames;
    public float    idleFps = 8f;

    [Header("Salida — motor")]
    [Tooltip("Frames del motor a máximo empuje al salir. Si vacío usa idleFrames.")]
    public Sprite[] thrustFrames;
    public float    thrustFps    = 14f;
    [Tooltip("Duración del movimiento de salida (segundos).")]
    public float    exitDuration = 1.0f;
    [Tooltip("Distancia horizontal hasta salir de pantalla (unidades de mundo).")]
    public float    exitDistance = 22f;

    public bool EntryComplete { get; private set; }
    public bool ExitComplete  { get; private set; }

    SpriteRenderer _sr;
    bool           _hovering;
    float          _hoverTime;
    float          _idleFrameTime;
    int            _idleFrameIdx;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (entryCurve == null || entryCurve.length == 0)
            BuildDefaultCurve();
    }

    void BuildDefaultCurve()
    {
        entryCurve = new AnimationCurve(
            new Keyframe(0f,    0f,    0f,    0f),
            new Keyframe(0.55f, 0.92f, 2.4f,  0.8f),
            new Keyframe(0.78f, 1.05f, 0.4f,  0.4f),
            new Keyframe(1f,    1f,    0f,    0f)
        );
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        transform.position = startPosition;
        EntryComplete      = false;
        _hovering          = false;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.position = startPosition;
        EntryComplete      = false;
        _hovering          = false;
    }

    public void PlayEntry()
    {
        StartCoroutine(EntryRoutine());
    }

    public void PlayExit()
    {
        _hovering    = false;
        ExitComplete = false;
        StartCoroutine(ExitRoutine());
    }

    IEnumerator EntryRoutine()
    {
        if (idleFrames != null && idleFrames.Length > 0 && _sr != null)
            _sr.sprite = idleFrames[0];

        float elapsed = 0f;
        while (elapsed < entryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / entryDuration);
            float v = entryCurve.Evaluate(t);
            transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, v);
            yield return null;
        }

        transform.position = targetPosition;
        EntryComplete      = true;
        _hovering          = true;
        _hoverTime         = 0f;
        _idleFrameTime     = 0f;
        _idleFrameIdx      = 1;
        if (idleFrames != null && idleFrames.Length > 1 && _sr != null)
            _sr.sprite = idleFrames[1];
    }

    IEnumerator ExitRoutine()
    {
        Vector3 startPos  = transform.position;
        Vector3 exitPos   = startPos + new Vector3(exitDistance, 0f, 0f);
        float   elapsed   = 0f;
        float   frameTime = 0f;
        int     frameIdx  = 0;

        Sprite[] exitFrames = (thrustFrames != null && thrustFrames.Length > 0)
                             ? thrustFrames : idleFrames;

        if (exitFrames != null && exitFrames.Length > 0 && _sr != null)
            _sr.sprite = exitFrames[0];

        while (elapsed < exitDuration)
        {
            elapsed   += Time.deltaTime;
            frameTime += Time.deltaTime;

            if (exitFrames != null && exitFrames.Length > 0 && _sr != null)
            {
                if (frameTime >= 1f / thrustFps)
                {
                    frameTime = 0f;
                    frameIdx  = (frameIdx + 1) % exitFrames.Length;
                    _sr.sprite = exitFrames[frameIdx];
                }
            }

            float t = Mathf.Clamp01(elapsed / exitDuration);
            float v = t * t; // aceleración cuadrática
            transform.position = Vector3.LerpUnclamped(startPos, exitPos, v);
            yield return null;
        }

        ExitComplete = true;
    }

    void Update()
    {
        if (!_hovering) return;

        _hoverTime += Time.deltaTime;
        float yOff = Mathf.Sin(_hoverTime * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = targetPosition + new Vector3(0f, yOff, 0f);

        if (idleFrames == null || idleFrames.Length == 0 || _sr == null) return;
        _idleFrameTime += Time.deltaTime;
        if (_idleFrameTime < 1f / idleFps) return;
        _idleFrameTime = 0f;

        // Loop forward from frame 1 onward (frame 0 = entry only)
        _idleFrameIdx++;
        if (_idleFrameIdx >= idleFrames.Length) _idleFrameIdx = 1;
        _sr.sprite = idleFrames[_idleFrameIdx];
    }
}
