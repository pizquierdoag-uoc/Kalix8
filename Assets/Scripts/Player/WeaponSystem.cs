using System.Collections.Generic;
using UnityEngine;

public class WeaponSystem : MonoBehaviour
{
    public enum WeaponType { Normal, Spread, Laser, Homing }

    [Header("Arma inicial")]
    public WeaponType currentWeapon = WeaponType.Normal;

    [Header("Cadencia (seg. entre disparos)")]
    public float fireRateNormal = 0.4f;
    public float fireRateSpread = 0.5f;
    public float fireRateHoming = 1.0f;

    [Header("Prefabs de balas")]
    public GameObject bulletNormalPrefab;
    public GameObject bulletSpreadPrefab;
    public GameObject bulletHomingPrefab;

    [Header("Spread")]
    public float spreadAngle = 18f;

    [Header("Láser")]
    public float    laserDamagePerSecond = 5f;
    public float    laserMaxLength       = 20f;
    public Color    laserColor           = new Color(0.2f, 0.9f, 1f, 1f);
    [Tooltip("Material para el LineRenderer del láser. Asignar en Inspector para que funcione en build.")]
    public Material laserMaterial;

    [Header("Tamaño de proyectiles")]
    public float bulletScale = 1.5f;

    // Nivel por arma (0=lv1, 1=lv2, 2=lv3). Orden: Normal, Spread, Laser, Homing
    readonly int[] _levels = { 0, 0, 0, 0 };

    int WeaponLevel => _levels[(int)currentWeapon];

    float        _fireCooldown;
    bool         _laserActive;
    LineRenderer _laserLine;
    Transform    _shootPoint;
    float        _laserDamageAccum;
    Collider2D   _laserLastTarget;

    readonly List<RaycastHit2D> _laserHits = new List<RaycastHit2D>();
    ContactFilter2D             _laserFilter;

    void Awake()
    {
        _laserFilter.useTriggers = true;

        _laserLine               = gameObject.AddComponent<LineRenderer>();
        _laserLine.positionCount = 2;
        _laserLine.startWidth    = 0.08f;
        _laserLine.endWidth      = 0.04f;
        if (laserMaterial != null)
        {
            _laserLine.material = new Material(laserMaterial);
        }
        else
        {
            // Shaders Built-in (Unlit/Color, Sprites/Default) no se incluyen en builds URP.
            // Buscar solo shaders URP; si tampoco están, crear material vacío en lugar de caer
            // en el shader de error magenta.
            Shader laserShader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                              ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (laserShader != null)
            {
                _laserLine.material = new Material(laserShader);
                _laserLine.material.SetFloat("_Surface", 1f);
            }
            else
            {
                // Sin material asignado en Inspector y sin shader URP disponible.
                // Usa un shader estándar disponible en cualquier proyecto para evitar
                // el material magenta de InternalErrorShader.
                var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));
                if (mat != null) mat.color = Color.clear; // invisible, no rompe el juego
                _laserLine.material = mat;
                Debug.LogWarning("[WeaponSystem] laserMaterial no asignado en Inspector. " +
                                 "El láser puede no verse en build. Asigna un material URP en el prefab.");
            }
        }
        _laserLine.startColor    = laserColor;
        _laserLine.endColor      = new Color(laserColor.r, laserColor.g, laserColor.b, 0f);
        _laserLine.useWorldSpace = true;

        // Renderizar por encima de los sprites del jugador
        var sr = GetComponent<SpriteRenderer>();
        _laserLine.sortingLayerName = sr != null ? sr.sortingLayerName : "Default";
        _laserLine.sortingOrder     = sr != null ? sr.sortingOrder + 2 : 10;

        _laserLine.enabled = false;
    }

    public void Shoot(Transform shootPoint)
    {
        _shootPoint = shootPoint;
        bool laserWasActive = _laserActive;
        if (currentWeapon != WeaponType.Laser) DeactivateLaser();
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
            if (!laserWasActive) AudioManager.Instance?.PlayLaserSFX();
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
        NotifyHUD();
    }

    public void OnPlayerDied()
    {
        int idx = (int)currentWeapon;
        _levels[idx] = Mathf.Max(_levels[idx] - 1, 0);
        NotifyHUD();
    }

    // Sube el nivel del arma indicada y cambia a ella (llamado por powerups de arma)
    public void UpgradeWeapon(WeaponType type)
    {
        DeactivateLaser();
        _levels[(int)type] = Mathf.Min(_levels[(int)type] + 1, 3);
        currentWeapon      = type;
        NotifyHUD();
    }

    public void SetWeapon(WeaponType type)
    {
        DeactivateLaser();
        currentWeapon = type;
        NotifyHUD();
    }

    void NotifyHUD() =>
        HUDController.Instance?.UpdateWeapon(currentWeapon.ToString().ToUpper(), WeaponLevel + 1);

    void ShootNormal()
    {
        _fireCooldown = fireRateNormal;
        switch (WeaponLevel)
        {
            case 0:
                SpawnBullet(bulletNormalPrefab, _shootPoint.position, Vector2.right);
                break;
            case 1:
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.up   * 0.15f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.down * 0.15f, Vector2.right);
                break;
            case 2:
                SpawnBullet(bulletNormalPrefab, _shootPoint.position,                        Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.up   * 0.28f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.down * 0.28f, Vector2.right);
                break;
            case 3:
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.up   * 0.12f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.down * 0.12f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.up   * 0.38f, Vector2.right);
                SpawnBullet(bulletNormalPrefab, _shootPoint.position + Vector3.down * 0.38f, Vector2.right);
                break;
        }
    }

    void ShootSpread()
    {
        _fireCooldown = fireRateSpread;
        int   count      = WeaponLevel == 0 ? 3 : WeaponLevel == 1 ? 4 : WeaponLevel == 2 ? 5 : 7;
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
        _laserLine.startWidth = (0.08f + WeaponLevel * 0.03f) * bulletScale;
        _laserLine.endWidth   = (0.04f + WeaponLevel * 0.015f) * bulletScale;

        Vector2 origin   = _shootPoint.position;
        float   maxDist  = laserMaxLength;
        if (Camera.main != null)
        {
            float screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f)).x;
            maxDist = Mathf.Max(maxDist, screenRight - origin.x + 2f);
        }
        Vector2 endPoint = origin + Vector2.right * maxDist;

        _laserHits.Clear();
        Physics2D.Raycast(origin, Vector2.right, _laserFilter, _laserHits, maxDist);

        RaycastHit2D bestHit  = default;
        float        bestDist = float.MaxValue;
        foreach (var h in _laserHits)
        {
            if (h.collider.CompareTag("Enemy") && h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit  = h;
            }
        }

        if (bestHit.collider != null)
        {
            // Si el láser apunta a un enemigo diferente, reseteamos el acumulador de daño
            if (bestHit.collider != _laserLastTarget)
            {
                _laserDamageAccum = 0f;
                _laserLastTarget  = bestHit.collider;
            }

            endPoint = bestHit.collider.bounds.center;
            float dps = laserDamagePerSecond * (1f + WeaponLevel * 0.4f);
            _laserDamageAccum += dps * Time.deltaTime;
            int dmg = Mathf.FloorToInt(_laserDamageAccum);
            if (dmg > 0)
            {
                var eh   = bestHit.collider.GetComponent<EnemyHealth>();
                var boss = bestHit.collider.GetComponent<BossController>();
                if (eh   != null) eh.TakeDamage(dmg);
                else if (boss != null) boss.TakeDamage(dmg);
                _laserDamageAccum -= dmg;
            }
        }
        else
        {
            _laserDamageAccum = 0f;
            _laserLastTarget  = null;
        }

        _laserLine.SetPosition(0, origin);
        _laserLine.SetPosition(1, endPoint);
    }

    void DeactivateLaser()
    {
        if (_laserLine != null) _laserLine.enabled = false;
        if (_laserActive) AudioManager.Instance?.StopLaserSFX();
        _laserActive      = false;
        _laserDamageAccum = 0f;
        _laserLastTarget  = null;
    }

    void ShootHoming()
    {
        _fireCooldown = fireRateHoming - WeaponLevel * 0.07f;
        int count = WeaponLevel == 0 ? 1 : WeaponLevel == 1 ? 1 : WeaponLevel == 2 ? 2 : 3;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = i == 0 ? Vector3.zero
                           : i == 1 ? Vector3.up   * 0.4f
                                    : Vector3.down  * 0.4f;
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
        if (_fireCooldown > 0f) _fireCooldown -= Time.deltaTime;
        if (_laserActive && currentWeapon != WeaponType.Laser) DeactivateLaser();
    }

    public WeaponType CurrentWeapon => currentWeapon;
    public int        CurrentLevel  => WeaponLevel;
}
