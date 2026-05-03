using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs de power-ups (arrastra los que quieras que suelte)")]
    public GameObject[] powerUpPrefabs;

    [Header("Probabilidad de soltar un power-up (0-1)")]
    [Range(0f, 1f)]
    public float dropChance = 0.25f;

    EnemyHealth _health;
    bool        _spawned;

    void Awake()
    {
        _health = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        _spawned = false;
    }

    void Update()
    {
        if (_spawned) return;
        if (_health == null) return;

        if (_health.IsDead)
        {
            _spawned = true;
            TrySpawn();
        }
    }

    void TrySpawn()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        // Elige un power-up aleatorio del array
        int idx = Random.Range(0, powerUpPrefabs.Length);
        if (powerUpPrefabs[idx] == null) return;

        Instantiate(powerUpPrefabs[idx], transform.position, Quaternion.identity);
    }
}
