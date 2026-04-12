using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShipShowcaseAnim : MonoBehaviour
{
    [Header("Posiciones (unidades de mundo)")]
    public Vector3 targetPosition = new Vector3(0f, -0.8f, 0f);
    public Vector3 startPosition  = new Vector3(0f, -9f,  0f); // Fuera de pantalla

    [Header("Animación de entrada")]
    public float          entryDuration = 2.0f;
    [Tooltip("Curva de movimiento. Por defecto se genera con overshoot.")]
    public AnimationCurve entryCurve;

    [Header("Flotación idle")]
    public float hoverAmplitude = 0.18f;  // Amplitud vertical (unidades)
    public float hoverFrequency = 0.60f;  // Ciclos por segundo

    [Header("Motor (opcional)")]
    [Tooltip("Arrastra aquí el FrameAnimator del motor/thruster de la nave.")]
    public FrameAnimator thrusterAnimator;

    public bool EntryComplete { get; private set; }

    bool  _hovering;
    float _hoverTime;

    void Awake()
    {
        if (entryCurve == null || entryCurve.length == 0)
            BuildDefaultCurve();
    }

    // Curva con spring-overshoot: llega rápido, se pasa, rebota al target
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
        if (thrusterAnimator != null)
        {
            thrusterAnimator.loop = true;
            thrusterAnimator.Play();
        }
        StartCoroutine(EntryRoutine());
    }

    IEnumerator EntryRoutine()
    {
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
    }

    void Update()
    {
        if (!_hovering) return;
        _hoverTime += Time.deltaTime;
        float yOff = Mathf.Sin(_hoverTime * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
        transform.position = targetPosition + new Vector3(0f, yOff, 0f);
    }
}
