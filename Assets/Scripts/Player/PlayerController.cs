using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed    = 6f;
    public float acceleration = 20f;

    [Header("Límites de pantalla")]
    public float boundsPaddingX = 0.5f;
    public float boundsPaddingY = 0.5f;

    [Header("Vida")]
    public int maxHealth = 3;

    [Header("Invulnerabilidad")]
    public float invulnerableDuration = 2.0f;
    public float blinkInterval        = 0.1f;

    [Header("Inclinación de la nave")]
    public float tiltAngle = 18f;
    public float tiltSpeed = 8f;

    [Header("Referencias")]
    public Transform  shootPoint;

    [Header("Explosión del jugador (hijo del prefab)")]
    [Tooltip("Arrastra aquí el GameObject hijo 'PlayerExplosion'")]
    public GameObject playerExplosionChild;

    int            _currentHealth;
    bool           _isInvulnerable;
    bool           _isDead;

    Rigidbody2D    _rb;
    SpriteRenderer _sr;
    WeaponSystem   _weaponSystem;

    Vector2 _velocity;
    Vector2 _input;
    float   _minX, _maxX, _minY, _maxY;

    void Awake()
    {
        _rb           = GetComponent<Rigidbody2D>();
        _sr           = GetComponent<SpriteRenderer>();
        _weaponSystem = GetComponent<WeaponSystem>();
    }

    void Start()
    {
        _currentHealth = maxHealth;
        CalculateCameraBounds();
    }

    void CalculateCameraBounds()
    {
        Camera cam  = Camera.main;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        _minX = cam.transform.position.x - halfW + boundsPaddingX;
        _maxX = cam.transform.position.x + halfW - boundsPaddingX;
        _minY = cam.transform.position.y - halfH + boundsPaddingY;
        _maxY = cam.transform.position.y + halfH - boundsPaddingY;
    }

    void Update()
    {
        if (_isDead) return;
        ReadInput();
        ApplyTilt();
        HandleShooting();
    }

    void FixedUpdate()
    {
        if (_isDead) return;
        MovePlayer();
    }

    void ReadInput()
    {
        var kb = Keyboard.current;
        _input.x = 0f;
        _input.y = 0f;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) _input.x -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) _input.x += 1f;
            if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) _input.y -= 1f;
            if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) _input.y += 1f;
        }
        if (_input.magnitude > 1f) _input.Normalize();
    }

    void MovePlayer()
    {
        Vector2 targetVelocity = _input * moveSpeed;
        _velocity = Vector2.MoveTowards(_velocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        Vector2 newPos = _rb.position + _velocity * Time.fixedDeltaTime;
        newPos.x = Mathf.Clamp(newPos.x, _minX, _maxX);
        newPos.y = Mathf.Clamp(newPos.y, _minY, _maxY);
        _rb.MovePosition(newPos);
    }

    void ApplyTilt()
    {
        float targetAngle  = -_input.y * tiltAngle;
        float currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;
        float smoothAngle = Mathf.Lerp(currentAngle, targetAngle, tiltSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, smoothAngle);
    }

    void HandleShooting()
    {
        if (_weaponSystem == null || shootPoint == null) return;
        var kb    = Keyboard.current;
        var mouse = Mouse.current;
        bool fire1Held     = (kb    != null && kb.leftCtrlKey.isPressed)        || (mouse != null && mouse.leftButton.isPressed);
        bool fire1Released = (kb    != null && kb.leftCtrlKey.wasReleasedThisFrame) || (mouse != null && mouse.leftButton.wasReleasedThisFrame);
        bool fire2Pressed  = (kb    != null && kb.leftAltKey.wasPressedThisFrame)   || (mouse != null && mouse.rightButton.wasPressedThisFrame);
        if (fire1Held)     _weaponSystem.Shoot(shootPoint);
        if (fire1Released) _weaponSystem.StopShooting();
        if (fire2Pressed)  _weaponSystem.CycleWeapon();

        bool bombPressed = kb != null && kb.spaceKey.wasPressedThisFrame;
        if (bombPressed)   PowerUpManager.Instance?.UseBomb();
    }

    public void TakeDamage(int amount = 1)
    {
        if (_isInvulnerable || _isDead) return;
        _currentHealth -= amount;
        AudioManager.Instance?.PlaySFX("player_hit");
        if (_currentHealth <= 0) Die();
        else StartCoroutine(InvulnerabilityRoutine());
    }

    void Die()
    {
        _isDead = true;
        _sr.enabled = false;
        AudioManager.Instance?.PlaySFX("player_death");

        // Activa la animación de explosión del jugador
        if (playerExplosionChild != null)
        {
            playerExplosionChild.SetActive(true);
            var fa = playerExplosionChild.GetComponent<FrameAnimator>();
            if (fa != null) { fa.loop = false; fa.Play(); }
        }

        GameManager.Instance?.PlayerDied();
    }

    public void Respawn(Vector2 spawnPosition)
    {
        _isDead        = false;
        _currentHealth = maxHealth;
        _velocity      = Vector2.zero;
        transform.position = spawnPosition;
        _sr.enabled        = true;
        if (playerExplosionChild != null) playerExplosionChild.SetActive(false);
        StartCoroutine(InvulnerabilityRoutine());
    }

    IEnumerator InvulnerabilityRoutine()
    {
        _isInvulnerable = true;
        float timer = 0f;
        while (timer < invulnerableDuration)
        {
            _sr.enabled = !_sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }
        _sr.enabled     = true;
        _isInvulnerable = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyBullet")) { TakeDamage(1); other.gameObject.SetActive(false); }
        if (other.CompareTag("Enemy"))       { TakeDamage(1); }
        if (other.CompareTag("PowerUp"))
        {
            other.gameObject.SetActive(false);
        }
    }

    public void SetInvulnerable(bool value) => _isInvulnerable = value;

    public void UpgradeWeapon() => _weaponSystem?.Upgrade();
    public void AddHealth(int amount) => _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);

    public int  CurrentHealth  => _currentHealth;
    public bool IsDead         => _isDead;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(_minX, _minY), new Vector3(_maxX, _minY));
        Gizmos.DrawLine(new Vector3(_maxX, _minY), new Vector3(_maxX, _maxY));
        Gizmos.DrawLine(new Vector3(_maxX, _maxY), new Vector3(_minX, _maxY));
        Gizmos.DrawLine(new Vector3(_minX, _maxY), new Vector3(_minX, _minY));
    }
#endif
}
