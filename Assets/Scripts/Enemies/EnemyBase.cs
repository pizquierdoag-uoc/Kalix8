using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;

    [Header("Bala enemiga")]
    public GameObject bulletPrefab;
    public float      bulletSpeed = 5f;

    [Header("Límites de zona de juego")]
    [Tooltip("Margen vertical respecto al borde de cámara para no invadir el scroll/hull.")]
    public float boundsPaddingY = 2.0f;

    protected EnemyHealth _health;
    protected Transform   _player;
    Rigidbody2D           _rb;
    protected float       _minY, _maxY;

    protected virtual void Awake()
    {
        _health      = GetComponent<EnemyHealth>();
        _rb          = GetComponent<Rigidbody2D>();
        float mult   = GameSettings.EnemySpeedMult;
        moveSpeed   *= mult;
        bulletSpeed *= mult;
        CalculateBounds();
    }

    void CalculateBounds()
    {
        Camera cam = Camera.main;
        if (cam == null) { _minY = -3f; _maxY = 3f; return; }
        float halfH = cam.orthographicSize;
        _minY = cam.transform.position.y - halfH + boundsPaddingY;
        _maxY = cam.transform.position.y + halfH - boundsPaddingY;
    }

    protected virtual void OnEnable()
    {
        // Busca al jugador al activarse
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    protected virtual void Update()
    {
        // Reintenta encontrar al jugador si spawneó mientras estaba inactivo (respawn)
        if (_player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }

        if (transform.position.x < -15f)
        {
            gameObject.SetActive(false);
            return;
        }

        // Clamp Y: el enemigo nunca invade la zona del scroll/hull
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, _minY, _maxY);
        transform.position = pos;
    }

    // Dispara una bala hacia el jugador
    protected void ShootAtPlayer()
    {
        if (bulletPrefab == null || _player == null) return;

        Vector2 dir = (_player.position - transform.position).normalized;
        GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);

        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = dir * bulletSpeed;

        // Rota la bala en la dirección de disparo
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        b.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Colisión con bala del jugador — fuente de verdad del daño
    void OnTriggerEnter2D(Collider2D other)
    {
        // Colisión con el casco/interior del scroll: el enemigo muere igual que el jugador
        if (other.CompareTag("Wall"))
        {
            _health?.TakeDamage(9999);
            return;
        }

        if (!other.CompareTag("PlayerBullet")) return;

        int dmg = 1;
        Bullet        b = other.GetComponent<Bullet>();
        HomingMissile m = other.GetComponent<HomingMissile>();
        if (b != null) dmg = b.damage;
        if (m != null) dmg = m.damage;

        Vector2 hitDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        if (_rb != null && _rb.bodyType != RigidbodyType2D.Static)
            _rb.AddForce(hitDir * 4f, ForceMode2D.Impulse);

        // Chispa de impacto
        HitSpark.Spawn(other.transform.position, Color.white);

        other.gameObject.SetActive(false);
        _health?.TakeDamage(dmg);   // score de muerte lo gestiona EnemyHealth.Die()
    }
}
