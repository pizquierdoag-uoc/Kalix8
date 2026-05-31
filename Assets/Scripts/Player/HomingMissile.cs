using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Propiedades")]
    public float speed       = 8f;
    public float rotateSpeed = 180f;
    public int   damage      = 2;
    public float lifetime    = 4f;

    [Header("Radio de búsqueda")]
    public float searchRadius = 12f;

    [Header("Motor — llama")]
    public Color  engineColorCore  = new Color(1f,   0.95f, 0.6f, 1f);  // blanco-amarillo
    public Color  engineColorOuter = new Color(1f,   0.4f,  0f,   1f);  // naranja
    public float  flameBaseLength  = 0.35f;
    public float  flamePulseAmount = 0.15f;
    public float  flamePulseSpeed  = 14f;
    public float  flameBaseWidth   = 0.12f;

    [Header("Motor — rastro")]
    public float  trailTime        = 0.25f;
    public float  trailStartWidth  = 0.08f;

    Transform    _target;
    float        _timer;
    float        _flameTimer;

    LineRenderer  _flame;
    TrailRenderer _trail;

    void Awake()
    {
        BuildFlame();
        // TrailRenderer eliminado: el bloom del URP lo hacía aparecer como un blob naranja grande
    }

    void BuildFlame()
    {
        var go = new GameObject("EngineFlame");
        go.transform.SetParent(transform, false);

        // Posición fija de la llama (cola del sprite del misil)
        go.transform.localPosition = new Vector3(-0.67f, 0.5f, 0f);

        _flame                   = go.AddComponent<LineRenderer>();
        _flame.useWorldSpace     = false;        // sigue la rotación del misil
        _flame.positionCount     = 3;
        _flame.startWidth        = flameBaseWidth;
        _flame.endWidth          = 0f;
        _flame.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _flame.receiveShadows    = false;

        // Gradiente: núcleo blanco-amarillo → naranja → transparente
        var grad    = new Gradient();
        var colors  = new GradientColorKey[]
        {
            new GradientColorKey(engineColorCore,  0f),
            new GradientColorKey(engineColorOuter, 0.5f),
            new GradientColorKey(engineColorOuter, 1f),
        };
        var alphas  = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f,  0f),
            new GradientAlphaKey(0.7f,0.5f),
            new GradientAlphaKey(0f,  1f),
        };
        grad.SetKeys(colors, alphas);
        _flame.colorGradient = grad;

        // Material: busca URP, si no usa el estándar disponible
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                 ?? Shader.Find("Sprites/Default")
                 ?? Shader.Find("Standard");
        if (sh != null) _flame.material = new Material(sh);

        var sr = GetComponent<SpriteRenderer>();
        _flame.sortingLayerName = sr != null ? sr.sortingLayerName : "Default";
        _flame.sortingOrder     = sr != null ? sr.sortingOrder - 1 : 0;
    }

    void BuildTrail()
    {
        _trail                   = gameObject.AddComponent<TrailRenderer>();
        _trail.time              = trailTime;
        _trail.startWidth        = trailStartWidth;
        _trail.endWidth          = 0f;
        _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _trail.receiveShadows    = false;
        _trail.generateLightingData = false;

        var grad   = new Gradient();
        var colors = new GradientColorKey[]
        {
            new GradientColorKey(engineColorOuter,           0f),
            new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 0.4f),
            new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1f),
        };
        var alphas = new GradientAlphaKey[]
        {
            new GradientAlphaKey(0.8f, 0f),
            new GradientAlphaKey(0.3f, 0.5f),
            new GradientAlphaKey(0f,   1f),
        };
        grad.SetKeys(colors, alphas);
        _trail.colorGradient = grad;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                 ?? Shader.Find("Sprites/Default")
                 ?? Shader.Find("Standard");
        if (sh != null) _trail.material = new Material(sh);

        var sr = GetComponent<SpriteRenderer>();
        _trail.sortingLayerName = sr != null ? sr.sortingLayerName : "Default";
        _trail.sortingOrder     = sr != null ? sr.sortingOrder - 1 : 0;
    }

    void OnEnable()
    {
        _timer      = 0f;
        _flameTimer = 0f;
        _target     = FindClosestEnemy();
        if (_trail != null) _trail.Clear();
    }

    void OnDisable()
    {
        if (_trail != null) _trail.Clear();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime) { gameObject.SetActive(false); return; }

        if (_target == null || !_target.gameObject.activeInHierarchy)
            _target = FindClosestEnemy();

        if (_target != null) SteerTowardsTarget();
        else FlyForward();

        UpdateFlame();
    }

    void UpdateFlame()
    {
        if (_flame == null) return;
        _flameTimer += Time.deltaTime;

        float pulse  = Mathf.Sin(_flameTimer * flamePulseSpeed) * flamePulseAmount
                     + Mathf.Sin(_flameTimer * flamePulseSpeed * 1.7f) * flamePulseAmount * 0.4f;
        float length = flameBaseLength + pulse;
        float width  = flameBaseWidth  + Mathf.Abs(pulse) * 0.5f;

        // 3 puntos: origen → mitad (con leve desplazamiento Y para dar forma) → extremo
        _flame.SetPosition(0, Vector3.zero);
        _flame.SetPosition(1, new Vector3(-length * 0.5f, Mathf.Sin(_flameTimer * flamePulseSpeed * 2.3f) * 0.02f, 0f));
        _flame.SetPosition(2, new Vector3(-length, 0f, 0f));
        _flame.startWidth = width;
    }

    void SteerTowardsTarget()
    {
        Vector2 toTarget  = _target.position - transform.position;
        float   angle     = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float   current   = transform.eulerAngles.z;
        float   newAngle  = Mathf.MoveTowardsAngle(current, angle, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        FlyForward();
    }

    void FlyForward() => transform.Translate(Vector2.right * speed * Time.deltaTime);

    Transform FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closestEnemy = null;
        float closestDist      = searchRadius;
        foreach (var enemy in enemies)
        {
            float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < closestDist) { closestDist = distanceToEnemy; closestEnemy = enemy.transform; }
        }
        return closestEnemy;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.GetComponent<EnemyBase>() == null)
                other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            gameObject.SetActive(false);
            return;
        }
        if (other.CompareTag("Wall") || other.CompareTag("EnemyBullet"))
        {
            gameObject.SetActive(false);
            return;
        }
        if (other.CompareTag("Player") || other.CompareTag("PlayerBullet")) return;
    }
}
