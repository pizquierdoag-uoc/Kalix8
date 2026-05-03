using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Propiedades")]
    public float speed    = 14f;
    public int   damage   = 1;
    public float lifetime = 3f;

    Vector2 _direction;
    float   _timer;

    public void Launch(Vector2 direction)
    {
        _direction   = direction.normalized;
        _timer       = 0f;
        transform.right = _direction;
    }

    void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
        _timer += Time.deltaTime;
        if (_timer >= lifetime) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // EnemyBase gestiona daño+knockback+chispa vía tag "PlayerBullet".
            // Solo aplicamos daño aquí si el enemigo no tiene EnemyBase (ej: boss).
            if (other.GetComponent<EnemyBase>() == null)
                other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            gameObject.SetActive(false);
            return;
        }
        if (other.CompareTag("Wall") || other.CompareTag("EnemyBullet"))
        {
            gameObject.SetActive(false);
            return;
        }
        if (other.CompareTag("Player") || other.CompareTag("PlayerBullet")) return;
    }
}
