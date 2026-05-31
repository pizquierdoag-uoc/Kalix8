using System.Collections;
using UnityEngine;

public class EnemyKamikaze : EnemyBase
{
    [Header("Kamikaze")]
    public float rotateSpeed = 200f;

    Vector2 _direction;
    bool    _locked;

    protected override void Awake()
    {
        base.Awake();
        // Velocidad final fija por dificultad (ignora EnemySpeedMult global)
        switch (GameSettings.CurrentDifficulty)
        {
            case GameSettings.Difficulty.Easy:   moveSpeed = 3.1f; break;  // 2.4 × 1.30 = +30%
            case GameSettings.Difficulty.Normal: moveSpeed = 3.2f; break;  // 2.7 × 1.20 = +20%
            case GameSettings.Difficulty.Hard:   moveSpeed = 3.3f; break;  // 3.0 × 1.10 = +10%
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _locked = false;
        StartCoroutine(LockDirection());
    }

    IEnumerator LockDirection()
    {
        yield return null;
        if (_player != null)
            _direction = (_player.position - transform.position).normalized;
        else
            _direction = Vector2.left;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        _locked = true;
    }

    protected override void Update()
    {
        if (!_locked) return;

        transform.Translate(_direction * moveSpeed * Time.deltaTime, Space.World);

        if (transform.position.x < -15f || Mathf.Abs(transform.position.y) > 12f)
            gameObject.SetActive(false);
    }
}
