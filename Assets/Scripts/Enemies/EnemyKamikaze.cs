using System.Collections;
using UnityEngine;

public class EnemyKamikaze : EnemyBase
{
    [Header("Kamikaze")]
    public float rotateSpeed = 200f;

    Vector2 _direction;
    bool    _locked;

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
        _locked = true;
    }

    protected override void Update()
    {
        if (!_locked) return;

        transform.Translate(_direction * moveSpeed * Time.deltaTime, Space.World);

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, angle),
            rotateSpeed * Time.deltaTime
        );

        if (transform.position.x < -15f || Mathf.Abs(transform.position.y) > 12f)
            gameObject.SetActive(false);
    }
}
