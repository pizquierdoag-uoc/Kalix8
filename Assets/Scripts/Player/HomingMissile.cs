using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    [Header("Propiedades")]
    public float speed       = 8f;
    public float rotateSpeed = 180f;
    public int   damage      = 2;
    public float lifetime    = 4f;

    [Header("Radio de búsqueda")]
    public float searchRadius = 12f;

    Transform _target;
    float     _timer;

    void OnEnable()
    {
        _timer  = 0f;
        _target = FindClosestEnemy();
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime) { gameObject.SetActive(false); return; }

        if (_target == null || !_target.gameObject.activeInHierarchy)
            _target = FindClosestEnemy();

        if (_target != null) SteerTowardsTarget();
        else FlyForward();
    }

    void SteerTowardsTarget()
    {
        Vector2 toTarget  = _target.position - transform.position;
        float   angle     = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        float   current   = transform.eulerAngles.z;
        float   newAngle  = Mathf.MoveTowardsAngle(current, angle, rotateSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        FlyForward();
    }

    void FlyForward() => transform.Translate(Vector2.right * speed * Time.deltaTime);

    Transform FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closest = null;
        float minDist     = searchRadius;
        foreach (var e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < minDist) { minDist = dist; closest = e.transform; }
        }
        return closest;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            ScoreManager.Instance?.AddScoreSmallEnemy();
            gameObject.SetActive(false);
        }
        if (other.CompareTag("Player") || other.CompareTag("PlayerBullet")) return;
    }
}
