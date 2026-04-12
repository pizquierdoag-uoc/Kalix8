using UnityEngine;

public class DroneOrbit : MonoBehaviour
{
    Transform  _target;
    float      _radius;
    float      _orbitSpeed;
    float      _currentAngle;
    float      _fireRate;
    float      _fireTimer;
    GameObject _bulletPrefab;

    SpriteRenderer _sr;

    public void Init(Transform target, float radius, float orbitSpeed,
                     float startAngle, float fireRate, GameObject bulletPrefab)
    {
        _target       = target;
        _radius       = radius;
        _orbitSpeed   = orbitSpeed;
        _currentAngle = startAngle;
        _fireRate     = fireRate;
        _bulletPrefab = bulletPrefab;
        _fireTimer    = Random.Range(0f, fireRate); // offset para que no disparen todos a la vez

        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _sr.color = new Color(0.6f, 1f, 0.6f);
    }

    void Update()
    {
        if (_target == null) { Destroy(gameObject); return; }

        // Orbita alrededor del jugador
        _currentAngle += _orbitSpeed * Time.deltaTime;
        if (_currentAngle >= 360f) _currentAngle -= 360f;

        float rad = _currentAngle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _radius;
        transform.position = _target.position + offset;

        // Rota para apuntar hacia la derecha (dirección de disparo)
        transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle + 90f);

        // Disparo automático
        _fireTimer += Time.deltaTime;
        if (_fireTimer >= _fireRate)
        {
            _fireTimer = 0f;
            ShootNearest();
        }
    }

    void ShootNearest()
    {
        if (_bulletPrefab == null) return;

        // Busca el enemigo más cercano
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest    = null;
        float     minDist    = 10f;

        foreach (var e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist) { minDist = dist; nearest = e.transform; }
        }

        Vector2 dir = nearest != null
            ? (nearest.position - transform.position).normalized
            : Vector2.right;

        GameObject b  = Instantiate(_bulletPrefab, transform.position, Quaternion.identity);
        Bullet     bl = b.GetComponent<Bullet>();
        if (bl != null) bl.Launch(dir);
    }
}
