using System.Collections.Generic;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    public enum WeaponType { Normal, Spread, Laser, Homing }

    [Header("Arma inicial")]
    public WeaponType currentWeapon = WeaponType.Normal;

    [Header("Cadencia (seg. entre disparos)")]
    public float fireRateNormal = 0.12f;
    public float fireRateSpread = 0.20f;
    public float fireRateHoming = 0.40f;

    [Header("Prefabs de balas")]
    public GameObject bulletNormalPrefab;
    public GameObject bulletSpreadPrefab;
    public GameObject bulletHomingPrefab;

    [Header("Spread")]
    public float spreadAngle = 18f;

    [Header("Láser")]
    public float laserDamagePerSecond = 5f;
    public float laserMaxLength       = 20f;
    public Color laserColor           = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("Tamaño de proyectiles")]
    public float bulletScale = 1.5f;

    [Header("Nivel de potencia (0-2)")]
    [Range(0,2)] public int weaponLevel = 0;

    float        _fireCooldown;
    bool         _laserActive;
    LineRenderer _laserLine;
    Transform    _shootPoint;
    float        _laserDamageAccum;

    void Awake()
    {
        _laserLine               = gameObject.AddComponent<LineRenderer>();
        _laserLine.positionCount = 2;
        _laserLine.startWidth    = 0.08f;
        _laserLine.endWidth      = 0.04f;
        Shader laserShader       = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                ?? Shader.Find("Sprites/Default");
        _laserLine.material      = new Material(laserShader);
        _laserLine.material.SetFloat("_Surface", 1f); // Transparent
        _laserLine.startColor    = laserColor;
        _laserLine.endColor      = new Color(laserColor.r, laserColor.g, laserColor.b, 0f);
        _laserLine.useWorldSpace = true;
        _laserLine.enabled       = false;
    }

    public void Shoot(Transform shootPoint)
    {
        _shootPoint = shootPoint;
        bool laserWasActive = _laserActive;
        if (currentWeapon != WeaponType.Laser) DeactivateLaser();
        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown > 0f) return;

        switch (currentWeapon)
        {
            case WeaponType.Normal: ShootNormal(); break;
            case WeaponType.Spread: ShootSpread(); break;
            case WeaponType.Laser:  ShootLaser();  break;
            case WeaponType.Homing: ShootHoming(); break;
        }

        if (currentWeapon == WeaponType.Laser)
        {
            if (!laserWasActive) AudioManager.Instance?.PlaySFX("shoot_laser");
        }
        else
        {
            AudioManager.Instance?.PlaySFX("shoot_" + currentWeapon.ToString().ToLower());
        }
    }

    public void StopShooting() => DeactivateLaser();

    public void CycleWeapon()
    {
        DeactivateLaser();
        currentWeapon = (WeaponType)(((int)currentWeapon + 1) % 4);
        weaponLevel   = 0;
        HUDController.Instance?.UpdateWeapon(currentWeapon.ToString().ToUpper(), weaponLevel);
    }

    public void Upgrade()
    {
        weaponLevel = Mathf.Min(weaponLevel + 1, 2);
        HUDController.Instance?.UpdateWeapon(currentWeapon.ToString().ToUpper(), weaponLevel);
    }

    public void SetWeapon(WeaponType type)
    {
        DeactivateLaser();
        currentWeapon = type;
        weaponLevel   = 0;
        HUDController.Instance?.UpdateWeapon(currentWeapon.ToString().ToUpper(), weaponLevel);
    }

    void ShootNormal()
    {
        _fireCooldown = fireRateNormal;
        switch (weaponLevel)
        {
            case 0: SpawnBullet(bulletNormalPrefab, _shootPoint.position, Vector2.right); break;
            case 1:
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.up   * 0.15f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.down * 0.15f, Vector2.right);
                break;
            case 2:
                SpawnBullet(bulletNormalPrefab, _shootPoint.position,                          Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.up   * 0.28f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.down * 0.28f, Vector2.right);
                break;
        }
    }

    void ShootSpread()
    {
        _fireCooldown = fireRateSpread;
        int   count      = weaponLevel == 0 ? 3 : weaponLevel == 1 ? 4 : 5;
        float totalAngle = spreadAngle * (count - 1);
        float startAngle = -totalAngle / 2f;
        for (int i = 0; i < count; i++)
        {
            float   angle = startAngle + spreadAngle * i;
            Vector2 dir   = Quaternion.Euler(0, 0, angle) * Vector2.right;
            SpawnBullet(bulletSpreadPrefab, _shootPoint.position, dir);
        }
    }

    void ShootLaser()
    {
        _fireCooldown      = 0f;
        _laserActive       = true;
        _laserLine.enabled = true;
        _laserLine.startWidth = (0.08f + weaponLevel * 0.06f) * bulletScale;
        _laserLine.endWidth   = (0.04f + weaponLevel * 0.03f) * bulletScale;

        Vector2 origin   = _shootPoint.position;
        Vector2 endPoint = origin + Vector2.right * laserMaxLength;

        // useTriggers=true porque los colliders de enemigos son triggers
        var filter  = new ContactFilter2D { useTriggers = true };
        var hits    = new List<RaycastHit2D>();
        Physics2D.Raycast(origin, Vector2.right, filter, hits, laserMaxLength);

        RaycastHit2D bestHit  = default;
        float        bestDist = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.collider.CompareTag("Enemy") && h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit  = h;
            }
        }

        if (bestHit.collider != null)
        {
            endPoint = bestHit.point;
            _laserDamageAccum += laserDamagePerSecond * Time.deltaTime;
            int dmg = Mathf.FloorToInt(_laserDamageAccum);
            if (dmg > 0)
            {
                bestHit.collider.GetComponent<EnemyHealth>()?.TakeDamage(dmg);
                _laserDamageAccum -= dmg;
            }
        }
        else
        {
            _laserDamageAccum = 0f;
        }

        _laserLine.SetPosition(0, origin);
        _laserLine.SetPosition(1, endPoint);
    }

    void DeactivateLaser()
    {
        if (_laserLine != null) _laserLine.enabled = false;
        _laserActive      = false;
        _laserDamageAccum = 0f;
    }

    void ShootHoming()
    {
        _fireCooldown = fireRateHoming - weaponLevel * 0.08f;
        int count = weaponLevel == 2 ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = i == 0 ? Vector3.zero : Vector3.up * 0.4f;
            SpawnBullet(bulletHomingPrefab, _shootPoint.position + offset, Vector2.right);
        }
    }

    void SpawnBullet(GameObject prefab, Vector3 position, Vector2 direction)
    {
        if (prefab == null) { Debug.LogWarning("[WeaponSystem] Prefab no asignado."); return; }
        GameObject obj = Instantiate(prefab, position, Quaternion.identity);
        obj.transform.localScale *= bulletScale;
        Bullet b = obj.GetComponent<Bullet>();
        if (b != null) b.Launch(direction);
    }

    void Update()
    {
        if (_laserActive && currentWeapon != WeaponType.Laser) DeactivateLaser();
    }

    public WeaponType CurrentWeapon => currentWeapon;
    public int        WeaponLevel   => weaponLevel;
}
