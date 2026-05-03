using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float lifetime = 4f;
    public int   damage   = 1;

    float _timer;

    void OnEnable()
    {
        _timer = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= lifetime)
            gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeLifeDamage();
            gameObject.SetActive(false);
            return;
        }
        if (other.CompareTag("Wall") || other.CompareTag("PlayerBullet"))
        {
            gameObject.SetActive(false);
            return;
        }
    }
}
