using System.Collections;
using UnityEngine;

public class EnemyShooter : EnemyBase
{
    [Header("Disparo")]
    public float fireRate       = 1.4f;   // disparos más frecuentes
    public float firstShotDelay = 0.3f;   // empieza a disparar antes

    [Header("Seguimiento")]
    [Tooltip("Velocidad vertical con la que persigue al jugador, como fracción de moveSpeed. 0 = sin seguimiento, 0.5 = la mitad de la velocidad horizontal.")]
    [Range(0f, 1.5f)]
    public float verticalTracking = 0.4f;

    [Tooltip("Solo persigue al jugador a partir de esta X (en world units). Más a la izquierda = el shooter ya está en zona de juego.")]
    public float trackingActivateX = 10f;

    [Header("Animación de disparo")]
    [Tooltip("Segundos previos al disparo en los que el sprite parpadea con un color de aviso (telégrafo).")]
    public float telegraphTime    = 0.25f;
    [Tooltip("Color del flash de aviso justo antes de disparar.")]
    public Color telegraphColor   = new Color(1f, 0.45f, 0.45f, 1f);
    [Tooltip("Multiplicador de escala al disparar (1.18 = +18% durante un instante).")]
    public float shootScalePunch  = 1.18f;
    [Tooltip("Tiempo en segundos que tarda en volver a su escala original tras el punch.")]
    public float shootRecoverTime = 0.12f;

    float          _fireTimer;
    SpriteRenderer _sr;
    Color          _baseColor;
    Vector3        _baseScale;
    bool           _telegraphed;
    Coroutine      _punchCo;

    protected override void Awake()
    {
        base.Awake();
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
        _baseScale = transform.localScale;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _fireTimer   = -firstShotDelay;
        _telegraphed = false;
        if (_sr != null) _sr.color = _baseColor;
        transform.localScale = _baseScale;
    }

    protected override void Update()
    {
        // Movimiento horizontal continuo hacia la izquierda
        Vector3 move = Vector3.left * moveSpeed * Time.deltaTime;

        // Seguimiento vertical suave del jugador (solo cuando el shooter ya está en pantalla)
        if (_player != null && verticalTracking > 0f && transform.position.x < trackingActivateX)
        {
            float dy   = _player.position.y - transform.position.y;
            float step = Mathf.Sign(dy) * moveSpeed * verticalTracking * Time.deltaTime;
            if (Mathf.Abs(dy) < Mathf.Abs(step)) step = dy;
            move.y += step;
        }

        transform.position += move;

        _fireTimer += Time.deltaTime;

        // Telégrafo: cambia a color de aviso en la ventana previa al disparo
        if (!_telegraphed && _fireTimer >= fireRate - telegraphTime && _sr != null)
        {
            _telegraphed = true;
            _sr.color    = telegraphColor;
        }

        // Disparo: restaura color, lanza punch de escala y dispara
        if (_fireTimer >= fireRate)
        {
            _fireTimer   = 0f;
            _telegraphed = false;
            if (_sr != null) _sr.color = _baseColor;

            ShootAtPlayer();

            if (_punchCo != null) StopCoroutine(_punchCo);
            _punchCo = StartCoroutine(ScalePunch());
        }

        base.Update();
    }

    IEnumerator ScalePunch()
    {
        transform.localScale = _baseScale * shootScalePunch;
        float t = 0f;
        while (t < shootRecoverTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / shootRecoverTime);
            transform.localScale = Vector3.Lerp(_baseScale * shootScalePunch, _baseScale, p);
            yield return null;
        }
        transform.localScale = _baseScale;
        _punchCo = null;
    }
}
