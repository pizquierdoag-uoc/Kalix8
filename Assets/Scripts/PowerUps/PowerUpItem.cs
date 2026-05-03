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

    [Header("Tamaño")]
    public float spriteScale = 1.5f;

    [Header("Color por tipo (se asigna automáticamente)")]
    public bool autoColor = true;

    SpriteRenderer _sr;
    float          _startY;
    float          _timer;
    float          _bobTimer;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        _startY    = transform.position.y;
        _timer     = 0f;
        _bobTimer  = 0f;
        transform.localScale = Vector3.one * spriteScale;

        if (autoColor && _sr != null)
            _sr.color = GetColorForType(type);
    }

    void Update()
    {
        // Avanza hacia la izquierda
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);

        // Movimiento flotante
        _bobTimer += Time.deltaTime;
        float y = transform.position.y;
        y = _startY + Mathf.Sin(_bobTimer * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, y, 0f);

        // Auto-destruye si nadie lo recoge
        _timer += Time.deltaTime;
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
                GameManager.Instance.AddScore(0); // refresca HUD
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
            case PowerUpType.Bomb:          return new Color(1f, 0.3f, 0.3f);
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
