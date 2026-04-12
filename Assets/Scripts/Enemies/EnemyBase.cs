using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 3f;

    [Header("Bala enemiga")]
    public GameObject bulletPrefab;
    public float      bulletSpeed = 5f;

    protected EnemyHealth _health;
    protected Transform   _player;

    protected virtual void Awake()
    {
        _health      = GetComponent<EnemyHealth>();
        float mult   = GameSettings.EnemySpeedMult;
        moveSpeed   *= mult;
        bulletSpeed *= mult;
    }

    protected virtual void OnEnable()
    {
        // Busca al jugador al activarse
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    protected virtual void Update()
    {
        // Si sale por la izquierda de la pantalla se desactiva
        if (transform.position.x < -15f)
            gameObject.SetActive(false);
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

    // Colisión con bala del jugador — delega en EnemyHealth
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            int dmg = 1;
            Bullet b = other.GetComponent<Bullet>();
            HomingMissile m = other.GetComponent<HomingMissile>();
            if (b != null) dmg = b.damage;
            if (m != null) dmg = m.damage;

            // Knockback: impulso en la dirección del impacto
            Vector2 hitDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null && rb.bodyType != RigidbodyType2D.Static)
                rb.AddForce(hitDir * 4f, ForceMode2D.Impulse);

            // Chispa de impacto
            HitSpark.Spawn(other.transform.position, Color.white);

            _health?.TakeDamage(dmg);
            other.gameObject.SetActive(false);
        }
    }
}
