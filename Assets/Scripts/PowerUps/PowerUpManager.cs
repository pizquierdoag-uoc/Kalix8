using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _bombCount = GameSettings.StartingBombs;
    }

    [Header("Escudo")]
    public float shieldDuration = 5f;
    public GameObject shieldEffect; // círculo visual opcional

    [Header("Speed Boost")]
    public float speedBoostStep     = 1.5f;  // velocidad extra por paso
    public int   speedBoostMaxSteps = 4;     // pasos máximos acumulables

    [Header("Bomba")]
    public int        startingBombs = 2;
    public GameObject bombExplosionEffect; // efecto visual opcional

    [Header("Debug — F3 (drop powerups)")]
    public GameObject[] debugPowerUpPrefabs;
    public int   debugDropCount = 4;

    [Header("Drones orbitales")]
    public GameObject dronePrefab;
    public int        droneCount       = 3;
    public float      droneOrbitRadius = 1.4f;
    public float      droneOrbitSpeed  = 120f; // grados/segundo
    public float      droneFireRate    = 1.2f;
    public GameObject droneBulletPrefab;

    List<GameObject> _activeDrones = new List<GameObject>();
    Coroutine        _shieldRoutine;
    int              _bombCount;
    GameObject       _shieldInstance;
    PlayerController _shieldedPlayer;
    int              _speedLevel;
    float            _baseSpeed;

    public int  BombCount    => _bombCount;
    public bool ShieldActive => _shieldInstance != null;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            DebugDropPowerUps();
    }

    void DebugDropPowerUps()
    {
        if (debugPowerUpPrefabs == null || debugPowerUpPrefabs.Length == 0)
        {
            Debug.LogWarning("[PowerUpManager] F3: asigna prefabs en 'Debug Power Up Prefabs'");
            return;
        }

        for (int i = 0; i < debugDropCount; i++)
        {
            int idx = Random.Range(0, debugPowerUpPrefabs.Length);
            if (debugPowerUpPrefabs[idx] == null) continue;

            Vector3 pos = new Vector3(
                Random.Range(2f, 7f),
                Random.Range(-3.5f, 3.5f),
                0f);
            Instantiate(debugPowerUpPrefabs[idx], pos, Quaternion.identity);
        }

        Debug.Log($"[PowerUpManager] F3 — spawneados {debugDropCount} powerups aleatorios");
    }

    public void ResetBombs(int count)
    {
        _bombCount = count;
        HUDController.Instance?.UpdateBombs(_bombCount);
    }

    public void AddBomb()
    {
        _bombCount++;
        HUDController.Instance?.UpdateBombs(_bombCount);
    }

    public void UseBomb()
    {
        if (_bombCount <= 0) return;
        _bombCount--;
        HUDController.Instance?.UpdateBombs(_bombCount);
        ActivateBomb();
    }

    public void ActivateShield(PlayerController player)
    {
        if (_shieldRoutine != null) StopCoroutine(_shieldRoutine);
        // Destruye el visual previo si el escudo se reactiva antes de expirar
        if (_shieldInstance != null) { Destroy(_shieldInstance); _shieldInstance = null; }
        _shieldRoutine = StartCoroutine(ShieldRoutine(player));
    }

    IEnumerator ShieldRoutine(PlayerController player)
    {
        _shieldedPlayer = player;
        player.SetInvulnerable(true);

        if (shieldEffect != null)
        {
            _shieldInstance = Instantiate(shieldEffect, player.transform);
            _shieldInstance.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("[PowerUpManager] shieldEffect no asignado en el Inspector");
        }

        yield return new WaitForSeconds(shieldDuration);

        DeactivateShield();
    }

    // Llamado por PlayerController cuando un golpe es absorbido por el escudo
    public void AbsorbShieldHit()
    {
        if (_shieldRoutine != null) { StopCoroutine(_shieldRoutine); _shieldRoutine = null; }
        DeactivateShield();
    }

    void DeactivateShield()
    {
        if (_shieldedPlayer != null) { _shieldedPlayer.SetInvulnerable(false); _shieldedPlayer = null; }
        if (_shieldInstance != null) { Destroy(_shieldInstance); _shieldInstance = null; }
    }

    public void ActivateSpeedBoost(PlayerController player)
    {
        if (_speedLevel == 0) _baseSpeed = player.moveSpeed;
        if (_speedLevel >= speedBoostMaxSteps) return;
        _speedLevel++;
        player.moveSpeed = _baseSpeed + _speedLevel * speedBoostStep;
        Debug.Log($"[SpeedBoost] nivel {_speedLevel}/{speedBoostMaxSteps}  speed={player.moveSpeed:F1}");
    }

    public void ResetSpeedBoost(PlayerController player)
    {
        if (_speedLevel > 0 && player != null)
            player.moveSpeed = _baseSpeed;
        _speedLevel = 0;
        _baseSpeed  = 0f;
    }

    public void ActivateBomb()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                ScoreManager.Instance?.AddScore(eh.scoreValue / 2);
                eh.TakeDamage(9999);
            }
            else
            {
                BossController boss = enemy.GetComponent<BossController>();
                boss?.TakeDamage(30);
            }
        }

        // Sonido de bomba
        AudioManager.Instance?.PlayBombExplosionSFX();

        // Explosiones en pantalla (comprobación Unity-safe contra objetos destruidos)
        if (BombScreenEffect.Instance != null)
            BombScreenEffect.Instance.Trigger();

        Debug.Log("BOMBA — " + enemies.Length + " enemigos eliminados");
    }

    public void ActivateOrbitDrones(PlayerController player)
    {
        // Si ya hay drones activos los elimina primero
        RemoveDrones();

        if (dronePrefab == null) return;

        for (int i = 0; i < droneCount; i++)
        {
            float   startAngle = i * (360f / droneCount);
            Vector3 offset     = Quaternion.Euler(0, 0, startAngle) * Vector3.up * droneOrbitRadius;

            GameObject drone = Instantiate(dronePrefab,
                player.transform.position + offset,
                Quaternion.identity);
            drone.transform.localScale = dronePrefab.transform.localScale * 5f;

            DroneOrbit orbitScript = drone.GetComponent<DroneOrbit>();
            if (orbitScript == null)
                orbitScript = drone.AddComponent<DroneOrbit>();

            orbitScript.Init(player.transform, droneOrbitRadius,
                             droneOrbitSpeed, startAngle,
                             droneFireRate, droneBulletPrefab);

            _activeDrones.Add(drone);
        }
    }

    public void RemoveDrones()
    {
        foreach (var d in _activeDrones)
            if (d != null) Destroy(d);
        _activeDrones.Clear();
    }

    IEnumerator DeactivateAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
}
