using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 3;
    public int scoreValue = 100;

    [Header("Drop de power-up")]
    [Range(0f, 1f)]
    public float dropChance = 0.18f;
    public GameObject[] dropPrefabs;

    [Header("Efectos (opcional)")]
    public ParticleSystem hitEffect;
    public ParticleSystem deathEffect;

    [Header("Explosión animada (opcional)")]
    public GameObject explosionPrefab;

    int  _currentHealth;
    bool _isDead;

    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        _currentHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * GameSettings.EnemyHealthMult));
        _isDead        = false;
        if (_sr != null) _sr.color = Color.white;
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;
        AudioManager.Instance?.PlaySFX("enemy_hit");

        if (_currentHealth <= 0) Die();
        else                     StartCoroutine(FlashRed());
    }

    void Die()
    {
        _isDead = true;
        ScoreManager.Instance?.AddScore(scoreValue);
        AudioManager.Instance?.PlaySFX("enemy_death");

        if (deathEffect != null)
            deathEffect.Play();

        ExplosionEffect.Spawn(explosionPrefab, transform.position);

        TryDropPowerUp();

        gameObject.SetActive(false);
    }

    IEnumerator FlashRed()
    {
        if (_sr == null) yield break;
        _sr.color = Color.red;
        yield return new WaitForSeconds(0.08f);
        _sr.color = Color.white;
    }

    void TryDropPowerUp()
    {
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        GameObject prefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
        if (prefab != null)
            Instantiate(prefab, transform.position, Quaternion.identity);
    }

    public int CurrentHealth => _currentHealth;
    public bool IsDead       => _isDead;
}
