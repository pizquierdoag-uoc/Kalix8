using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Vida")]
    public int   maxHealth  = 200;
    public int   scoreValue = 50000;

    [Header("Movimiento")]
    public float entrySpeed    = 3f;    // velocidad al entrar en pantalla
    public float entryTargetX  = 4f;    // posición X donde se detiene
    public float bobAmplitude  = 2.5f;  // altura del movimiento vertical
    public float bobFrequency  = 0.8f;  // velocidad del movimiento vertical

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public float      bulletSpeed      = 5f;
    public float      fireRatePhase1   = 1.5f;
    public float      fireRatePhase2   = 0.6f;

    [Header("Barra de vida del boss (opcional)")]
    public UnityEngine.UI.Slider healthBar;

    int   _currentHealth;
    bool  _isDead;
    bool  _isEntering = true;
    bool  _phase2Active;
    float _fireTimer;
    float _bobTime;

    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        _currentHealth = maxHealth;
        _isDead        = false;
        _isEntering    = true;
        _phase2Active  = false;
        _fireTimer     = 0f;
        _bobTime       = 0f;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value    = maxHealth;
            healthBar.gameObject.SetActive(true);
        }

        // Música de boss
        AudioManager.Instance?.PlayBossMusic();
    }

    void Update()
    {
        if (_isDead) return;

        if (_isEntering)
        {
            EnterScreen();
            return;
        }

        BobMovement();
        HandleShooting();
    }

    void EnterScreen()
    {
        transform.Translate(Vector2.left * entrySpeed * Time.deltaTime);
        if (transform.position.x <= entryTargetX)
        {
            Vector3 pos = transform.position;
            pos.x = entryTargetX;
            transform.position = pos;
            _isEntering = false;
        }
    }

    void BobMovement()
    {
        _bobTime += Time.deltaTime;
        float speed = _phase2Active ? bobFrequency * 1.5f : bobFrequency;
        float y = Mathf.Sin(_bobTime * speed) * bobAmplitude;
        transform.position = new Vector3(entryTargetX, y, 0f);
    }

    void HandleShooting()
    {
        float rate = _phase2Active ? fireRatePhase2 : fireRatePhase1;
        _fireTimer += Time.deltaTime;

        if (_fireTimer >= rate)
        {
            _fireTimer = 0f;
            if (_phase2Active) ShootPhase2();
            else               ShootPhase1();
        }
    }

    // Fase 1 — abanico de 5 balas
    void ShootPhase1()
    {
        float[] angles = { 170f, 185f, 200f, 155f, 215f };
        foreach (float angle in angles)
        {
            Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.right;
            SpawnBullet(dir);
        }
    }

    // Fase 2 — ráfaga en 3 filas + disparo directo al jugador
    void ShootPhase2()
    {
        // 3 filas horizontales
        SpawnBullet(Vector2.left);
        SpawnBullet(Quaternion.Euler(0,0,15f)  * Vector2.left);
        SpawnBullet(Quaternion.Euler(0,0,-15f) * Vector2.left);

        // Disparo directo al jugador
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            Vector2 dir = (p.transform.position - transform.position).normalized;
            SpawnBullet(dir);
        }
    }

    void SpawnBullet(Vector2 direction)
    {
        if (bulletPrefab == null) return;
        GameObject b  = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = direction * bulletSpeed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        b.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        AudioManager.Instance?.PlaySFX("enemy_hit");

        if (healthBar != null)
            healthBar.value = _currentHealth;

        // Activa fase 2 al llegar al 50%
        if (!_phase2Active && _currentHealth <= maxHealth / 2)
            ActivatePhase2();

        StartCoroutine(FlashRed());

        if (_currentHealth <= 0)
            Die();
    }

    void ActivatePhase2()
    {
        _phase2Active = true;
        Debug.Log("BOSS — Fase 2 activada");
        // Aquí puedes añadir efectos visuales de transición de fase
    }

    void Die()
    {
        _isDead = true;
        AudioManager.Instance?.PlaySFX("boss_death");
        ScoreManager.Instance?.AddScore(scoreValue);

        if (healthBar != null)
            healthBar.gameObject.SetActive(false);

        Debug.Log("BOSS DERROTADO");

        // Inicia la secuencia de final de fase
        StageClearManager.Instance?.TriggerStageClear();

        gameObject.SetActive(false);
    }


    IEnumerator FlashRed()
    {
        if (_sr == null) yield break;
        _sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        _sr.color = Color.white;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            int dmg = 1;
            Bullet b        = other.GetComponent<Bullet>();
            HomingMissile m = other.GetComponent<HomingMissile>();
            if (b != null) dmg = b.damage;
            if (m != null) dmg = m.damage;

            TakeDamage(dmg);
            other.gameObject.SetActive(false);
        }
    }

    public int  CurrentHealth => _currentHealth;
    public bool IsPhase2      => _phase2Active;
}
