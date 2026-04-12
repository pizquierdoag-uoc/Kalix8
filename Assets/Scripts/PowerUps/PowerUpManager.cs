using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    [Header("Escudo")]
    public float shieldDuration = 5f;
    public GameObject shieldEffect; // círculo visual opcional

    [Header("Speed Boost")]
    public float speedBoostDuration    = 6f;
    public float speedBoostMultiplier  = 1.6f;

    [Header("Bomba")]
    public GameObject bombExplosionEffect; // efecto visual opcional

    [Header("Drones orbitales")]
    public GameObject dronePrefab;
    public int        droneCount       = 3;
    public float      droneOrbitRadius = 1.4f;
    public float      droneOrbitSpeed  = 120f; // grados/segundo
    public float      droneFireRate    = 1.2f;
    public GameObject droneBulletPrefab;

    List<GameObject> _activeDrones = new List<GameObject>();
    Coroutine        _shieldRoutine;
    Coroutine        _speedRoutine;

    public void ActivateShield(PlayerController player)
    {
        if (_shieldRoutine != null) StopCoroutine(_shieldRoutine);
        _shieldRoutine = StartCoroutine(ShieldRoutine(player));
    }

    IEnumerator ShieldRoutine(PlayerController player)
    {
        // Activa invulnerabilidad directamente
        var invField = typeof(PlayerController)
            .GetField("_isInvulnerable",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

        if (invField != null) invField.SetValue(player, true);

        if (shieldEffect != null)
        {
            shieldEffect.transform.SetParent(player.transform);
            shieldEffect.transform.localPosition = Vector3.zero;
            shieldEffect.SetActive(true);
        }

        yield return new WaitForSeconds(shieldDuration);

        if (invField != null) invField.SetValue(player, false);
        if (shieldEffect != null) shieldEffect.SetActive(false);
    }

    public void ActivateSpeedBoost(PlayerController player)
    {
        if (_speedRoutine != null) StopCoroutine(_speedRoutine);
        _speedRoutine = StartCoroutine(SpeedBoostRoutine(player));
    }

    IEnumerator SpeedBoostRoutine(PlayerController player)
    {
        float original = player.moveSpeed;
        player.moveSpeed = original * speedBoostMultiplier;

        yield return new WaitForSeconds(speedBoostDuration);

        player.moveSpeed = original;
    }

    public void ActivateBomb()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                // Suma puntos por cada enemigo eliminado
                ScoreManager.Instance?.AddScore(eh.scoreValue / 2);
                eh.TakeDamage(9999);
            }
            else
            {
                // Boss — le hace daño fuerte pero no lo mata de un golpe
                BossController boss = enemy.GetComponent<BossController>();
                boss?.TakeDamage(30);
            }
        }

        if (bombExplosionEffect != null)
        {
            bombExplosionEffect.transform.position = Vector3.zero;
            bombExplosionEffect.SetActive(true);
            StartCoroutine(DeactivateAfter(bombExplosionEffect, 1.5f));
        }

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
