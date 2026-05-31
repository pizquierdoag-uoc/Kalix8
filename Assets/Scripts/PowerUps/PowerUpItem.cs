using System.Collections;
using UnityEngine;

public enum PowerUpType
{
    WeaponNormal,
    WeaponSpread,
    WeaponLaser,
    WeaponHoming,
    ExtraLife,
    Shield,
    SpeedBoost,
    Bomb,
    OrbitDrones
}

public class PowerUpItem : MonoBehaviour
{
    [Header("Tipo")]
    public PowerUpType type;

    [Header("Movimiento")]
    public float moveSpeed   = 2f;
    public float bobAmount   = 0.3f;
    public float bobSpeed    = 2f;
    public float lifetime    = 12f;

    [Header("Rotación")]
    public bool  rotate      = true;
    public float rotateSpeed = 45f;   // grados/segundo (negativo = sentido contrario)

    [Header("Respiración (escala pulsante)")]
    public bool  breathe       = true;
    public float breatheAmount = 0.08f;  // variación máxima de escala sobre spriteScale
    public float breatheSpeed  = 2f;     // ciclos por segundo

    [Header("Tamaño")]
    public float spriteScale = 1.5f;

    [Header("Color por tipo (se asigna automáticamente)")]
    public bool autoColor = true;

    SpriteRenderer _sr;
    float          _startY;
    float          _timer;
    float          _bobTimer;
    float          _breatheTimer;
    float          _baseScale;      // escala normalizada, base del efecto breathe

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        _startY       = transform.position.y;
        _timer        = 0f;
        _bobTimer     = 0f;
        _breatheTimer = 0f;
        transform.localRotation = Quaternion.identity;

        // Usa un tamaño de referencia fijo (todos los sprites hex son 27px a 100PPU = 0.27 wu).
        // No leemos _sr.sprite.bounds porque FrameAnimator puede no haber inicializado
        // el sprite correcto todavía (race condition entre Awake/OnEnable).
        const float kRefNativeWidth = 0.27f;
        _baseScale = spriteScale / kRefNativeWidth;
        transform.localScale = Vector3.one * _baseScale;

        if (autoColor && _sr != null)
            _sr.color = GetColorForType(type);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // Avanza hacia la izquierda
        transform.Translate(Vector2.left * moveSpeed * dt);

        // Movimiento flotante vertical
        _bobTimer += dt;
        float y = _startY + Mathf.Sin(_bobTimer * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, y, 0f);

        // Rotación continua (opcional por prefab)
        if (rotate)
            transform.Rotate(0f, 0f, rotateSpeed * dt);

        // Respiración: escala pulsante (opcional por prefab)
        if (breathe)
        {
            _breatheTimer += dt;
            float pulse = Mathf.Sin(_breatheTimer * breatheSpeed * Mathf.PI * 2f) * breatheAmount;
            float s     = _baseScale + pulse;
            transform.localScale = new Vector3(s, s, 1f);
        }

        // Auto-destruye si nadie lo recoge
        _timer += dt;
        if (_timer >= lifetime || transform.position.x < -14f)
            gameObject.SetActive(false);
    }

    // Aplica el efecto al jugador
    public void Apply(PlayerController player)
    {
        if (player == null) return;

        switch (type)
        {
            case PowerUpType.WeaponNormal:
                player.GetComponent<WeaponSystem>()?.UpgradeWeapon(WeaponSystem.WeaponType.Normal);
                break;
            case PowerUpType.WeaponSpread:
                player.GetComponent<WeaponSystem>()?.UpgradeWeapon(WeaponSystem.WeaponType.Spread);
                break;
            case PowerUpType.WeaponLaser:
                player.GetComponent<WeaponSystem>()?.UpgradeWeapon(WeaponSystem.WeaponType.Laser);
                break;
            case PowerUpType.WeaponHoming:
                player.GetComponent<WeaponSystem>()?.UpgradeWeapon(WeaponSystem.WeaponType.Homing);
                break;
            case PowerUpType.ExtraLife:
                player.AddHealth(1);
                HUDController.Instance?.UpdateLives(player.CurrentHealth);
                break;
            case PowerUpType.Shield:
                PowerUpManager.Instance?.ActivateShield(player);
                break;
            case PowerUpType.SpeedBoost:
                PowerUpManager.Instance?.ActivateSpeedBoost(player);
                break;
            case PowerUpType.Bomb:
                PowerUpManager.Instance?.AddBomb();
                break;
            case PowerUpType.OrbitDrones:
                PowerUpManager.Instance?.ActivateOrbitDrones(player);
                break;
        }

        AudioManager.Instance?.PlaySFX("powerup");
    }

    // Color identificativo por tipo
    Color GetColorForType(PowerUpType t)
    {
        switch (t)
        {
            case PowerUpType.WeaponNormal:  return new Color(1f, 0.85f, 0.1f);
            case PowerUpType.WeaponSpread:  return new Color(0.2f, 1f, 0.3f);
            case PowerUpType.WeaponLaser:   return new Color(0.3f, 0.85f, 1f);
            case PowerUpType.WeaponHoming:  return new Color(1f, 0.35f, 0.25f);
            case PowerUpType.ExtraLife:     return new Color(0.2f, 1f, 0.4f);
            case PowerUpType.Shield:        return new Color(0.4f, 0.8f, 1f);
            case PowerUpType.SpeedBoost:    return Color.white;
            case PowerUpType.Bomb:          return Color.white;
            case PowerUpType.OrbitDrones:   return Color.white;
            default:                        return Color.white;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Apply(other.GetComponent<PlayerController>());
            gameObject.SetActive(false);
        }
    }
}
